using Diffinitely.Models;
using Octokit;
using System.Diagnostics;
using System.Text;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Diffinitely.Services
{
    internal sealed class ReviewThreadMutationResult
    {
        private ReviewThreadMutationResult(bool succeeded, string? errorMessage)
        {
            Succeeded = succeeded;
            ErrorMessage = errorMessage;
        }

        public bool Succeeded { get; }

        public string? ErrorMessage { get; }

        public static ReviewThreadMutationResult Success() => new(true, null);

        public static ReviewThreadMutationResult Failure(string message) => new(false, message);
    }

    internal class GitHubPullRequestService
    {
        private static readonly HttpClient _graphQlHttpClient = new();
        private readonly GitRepositoryService _repoService = new();
        private readonly GitHubClient _client = new(new ProductHeaderValue("DiffinitelyPRHelper"));
        private static string _cachedToken = string.Empty; // in-memory cache

        private async Task<bool> EnsureCredentialsAsync(CancellationToken ct)
        {
            if (_client.Credentials != null && !string.IsNullOrEmpty(_client.Credentials.Password)) return true;
            if (!string.IsNullOrEmpty(_cachedToken))
            {
                _client.Credentials = new Credentials(_cachedToken);
                return true;
            }

            // Acquire via git credential fill
            var acquired = TryAcquireViaGitCredentialManager(out var token);
            if (acquired && !string.IsNullOrEmpty(token))
            {
                _cachedToken = token;
                _client.Credentials = new Credentials(token);
                return true;
            }
            return false;
        }

        private bool TryAcquireViaGitCredentialManager(out string token)
        {
            token = string.Empty;
            try
            {
                var psi = new ProcessStartInfo("git")
                {
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments = "credential fill"
                };
                using var p = Process.Start(psi);
                if (p == null) return false;

                var sb = new StringBuilder();
                sb.AppendLine("protocol=https");
                sb.AppendLine("host=github.com");
                sb.AppendLine();
                p.StandardInput.Write(sb.ToString());
                p.StandardInput.Flush();
                p.StandardInput.Close();

                var output = p.StandardOutput.ReadToEnd();
                p.WaitForExit(3000);
                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.StartsWith("password="))
                    {
                        token = trimmed.Substring("password=".Length).Trim();
                        break;
                    }
                }
                return !string.IsNullOrEmpty(token);
            }
            catch
            {
                return false;
            }
        }

        public virtual async Task<PullRequestInfo> GetCurrentBranchPullRequestAsync(CancellationToken ct)
        {
            // Resolve repo info from local .git metadata.
            string repoRoot; try { repoRoot = _repoService.FindRepoRoot(System.Environment.CurrentDirectory); } catch { return null; }
            var branch = _repoService.GetCurrentBranch(repoRoot);
            if (string.IsNullOrEmpty(branch)) return null;
            if (!_repoService.TryGetRemoteOriginUrl(repoRoot, out var owner, out var repo)) return null;
            if (string.IsNullOrEmpty(owner) || string.IsNullOrEmpty(repo)) return null;

            // Ensure we have credentials; if not, proceed unauthenticated (rate-limited) but attempt anyway.
            await EnsureCredentialsAsync(ct);

            var request = new PullRequestRequest { State = ItemStateFilter.All, Head = owner + ":" + branch };

            var prs = await _client.PullRequest.GetAllForRepository(owner, repo, request);
            var pr = prs.Count > 0 ? prs[0] : null; if (pr == null) return null;
            var comments = await _client.PullRequest.ReviewComment.GetAll(owner, repo, pr.Number);
            var files = await _client.PullRequest.Files(owner, repo, pr.Number);
            var changed = new List<ChangedFileInfo>();

            foreach (var f in files)
            {
                var kind = ChangeKind.Modified;
                switch (f.Status) { case "added": kind = ChangeKind.Added; break; case "removed": kind = ChangeKind.Deleted; break; case "renamed": kind = ChangeKind.Renamed; break; }
                var numberOfComments = comments.Count(c => c.Path == f.FileName);
                changed.Add(new ChangedFileInfo { CommentCount = numberOfComments, FullPath = $"{repoRoot}\\{f.FileName}", Path = f.FileName, PreviousPath = f.PreviousFileName, Kind = kind });
            }
            var reviewThreads = await GetReviewThreadsAsync(owner, repo, pr.Number, ct);
            return new PullRequestInfo { Comments = comments, Id = pr.Number.ToString(), Title = pr.Title, ChangedFiles = changed, Owner = owner, Repository = repo, BaseSha = pr.Base?.Sha, HeadSha = pr.Head?.Sha, RepoRoot = repoRoot, ReviewThreads = reviewThreads };
        }

        public async Task<string?> GetFileContentAsync(string owner, string repo, string path, string sha, CancellationToken ct)
        {
            try
            {
                var blobRef = await _client.Repository.Content.GetAllContentsByRef(owner, repo, path, sha);
                var first = blobRef.FirstOrDefault();
                return first?.Content; // API returns Base64 decoded content
            }
            catch { return null; }
        }

        internal virtual async Task<ReviewThreadMutationResult> ResolveReviewThreadAsync(string reviewThreadId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reviewThreadId))
            {
                return ReviewThreadMutationResult.Failure("The review thread is missing its GitHub thread ID.");
            }

            if (!await EnsureCredentialsAsync(ct))
            {
                return ReviewThreadMutationResult.Failure("GitHub authentication is unavailable, so the review thread could not be resolved.");
            }

            var query = "mutation($threadId:ID!){resolveReviewThread(input:{threadId:$threadId}){thread{id isResolved}}}";

            try
            {
                using var response = await PostGraphQlAsync(query, new { threadId = reviewThreadId }, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return ReviewThreadMutationResult.Failure($"GitHub returned {(int)response.StatusCode} ({response.ReasonPhrase}) while resolving the review thread.");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (TryGetGraphQlError(doc.RootElement, out var errorMessage))
                {
                    return ReviewThreadMutationResult.Failure(errorMessage);
                }

                if (!TryGetResolvedThread(doc.RootElement, out var resolvedThreadId, out var isResolved) ||
                    !isResolved ||
                    !string.Equals(resolvedThreadId, reviewThreadId, StringComparison.Ordinal))
                {
                    return ReviewThreadMutationResult.Failure("GitHub did not confirm that the review thread was resolved.");
                }

                return ReviewThreadMutationResult.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ReviewThreadMutationResult.Failure($"Resolving the review thread failed: {ex.Message}");
            }
        }

        internal virtual async Task<ReviewThreadMutationResult> UnresolveReviewThreadAsync(string reviewThreadId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(reviewThreadId))
            {
                return ReviewThreadMutationResult.Failure("The review thread is missing its GitHub thread ID.");
            }

            if (!await EnsureCredentialsAsync(ct))
            {
                return ReviewThreadMutationResult.Failure("GitHub authentication is unavailable, so the review thread could not be unresolved.");
            }

            var query = "mutation($threadId:ID!){unresolveReviewThread(input:{threadId:$threadId}){thread{id isResolved}}}";

            try
            {
                using var response = await PostGraphQlAsync(query, new { threadId = reviewThreadId }, ct);
                if (!response.IsSuccessStatusCode)
                {
                    return ReviewThreadMutationResult.Failure($"GitHub returned {(int)response.StatusCode} ({response.ReasonPhrase}) while unresolving the review thread.");
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);

                if (TryGetGraphQlError(doc.RootElement, out var errorMessage))
                {
                    return ReviewThreadMutationResult.Failure(errorMessage);
                }

                if (!TryGetUnresolvedThread(doc.RootElement, out var unresolvedThreadId, out var isResolved) ||
                    isResolved ||
                    !string.Equals(unresolvedThreadId, reviewThreadId, StringComparison.Ordinal))
                {
                    return ReviewThreadMutationResult.Failure("GitHub did not confirm that the review thread was unresolved.");
                }

                return ReviewThreadMutationResult.Success();
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return ReviewThreadMutationResult.Failure($"Unresolving the review thread failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Queries the GitHub GraphQL API to get review-thread node IDs and resolved state.
        /// Returns a dictionary keyed by the top-level review comment database ID.
        /// </summary>
        internal async Task<Dictionary<long, PullRequestReviewThread>> GetReviewThreadsAsync(
            string owner, string repo, int prNumber, CancellationToken ct)
        {
            var result = new Dictionary<long, PullRequestReviewThread>();

            if (!await EnsureCredentialsAsync(ct))
                return result;

            try
            {
                string? cursor = null;
                bool hasNextPage;
                do
                {
                    var query = "query($owner:String!,$repo:String!,$number:Int!,$cursor:String){repository(owner:$owner,name:$repo){pullRequest(number:$number){reviewThreads(first:100,after:$cursor){nodes{id isResolved comments(first:1){nodes{databaseId}}} pageInfo{hasNextPage endCursor}}}}}";
                    using var response = await PostGraphQlAsync(query, new { owner, repo, number = prNumber, cursor }, ct);
                    if (!response.IsSuccessStatusCode) return result;

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(json);
                    if (TryGetGraphQlError(doc.RootElement, out _)) return result;
                    if (!TryGetReviewThreadsNode(doc.RootElement, out var reviewThreads)) return result;
                    if (!reviewThreads.TryGetProperty("nodes", out var nodes)) return result;

                    foreach (var thread in nodes.EnumerateArray())
                    {
                        if (!thread.TryGetProperty("id", out var threadIdProp) ||
                            threadIdProp.ValueKind != JsonValueKind.String)
                        {
                            continue;
                        }

                        if (!thread.TryGetProperty("isResolved", out var isResolvedProp))
                        {
                            continue;
                        }

                        if (!thread.TryGetProperty("comments", out var comments) ||
                            !comments.TryGetProperty("nodes", out var commentNodes))
                        {
                            continue;
                        }

                        foreach (var comment in commentNodes.EnumerateArray())
                        {
                            if (comment.TryGetProperty("databaseId", out var dbIdProp) &&
                                dbIdProp.ValueKind == JsonValueKind.Number)
                            {
                                result[dbIdProp.GetInt64()] = new PullRequestReviewThread
                                {
                                    Id = threadIdProp.GetString() ?? string.Empty,
                                    IsResolved = isResolvedProp.GetBoolean()
                                };
                            }
                        }
                    }

                    hasNextPage = TryGetPageInfo(reviewThreads, out cursor);
                }
                while (hasNextPage);
            }
            catch { /* best-effort; fall back to all-unresolved */ }

            return result;
        }

        private async Task<HttpResponseMessage> PostGraphQlAsync(string query, object variables, CancellationToken ct)
        {
            var token = _cachedToken;
            if (string.IsNullOrEmpty(token))
            {
                token = _client.Credentials?.Password;
            }

            var requestBody = JsonSerializer.Serialize(new
            {
                query,
                variables
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.github.com/graphql");
            request.Headers.UserAgent.ParseAdd("DiffinitelyPRHelper");
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("token", token);
            }

            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            return await _graphQlHttpClient.SendAsync(request, ct);
        }

        private static bool TryGetGraphQlError(JsonElement root, out string message)
        {
            message = string.Empty;
            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind != JsonValueKind.Array || errors.GetArrayLength() == 0)
            {
                return false;
            }

            var messages = new List<string>();
            foreach (var error in errors.EnumerateArray())
            {
                if (error.TryGetProperty("message", out var errorMessage) && errorMessage.ValueKind == JsonValueKind.String)
                {
                    messages.Add(errorMessage.GetString() ?? string.Empty);
                }
            }

            message = string.Join(" ", messages.Where(m => !string.IsNullOrWhiteSpace(m)));
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "GitHub returned a GraphQL error.";
            }

            return true;
        }

        private static bool TryGetReviewThreadsNode(JsonElement root, out JsonElement reviewThreads)
        {
            reviewThreads = default;
            if (!root.TryGetProperty("data", out var data)) return false;
            if (!data.TryGetProperty("repository", out var repository)) return false;
            if (!repository.TryGetProperty("pullRequest", out var pullRequest)) return false;
            if (!pullRequest.TryGetProperty("reviewThreads", out reviewThreads)) return false;
            return true;
        }

        private static bool TryGetPageInfo(JsonElement reviewThreads, out string? endCursor)
        {
            endCursor = null;
            if (!reviewThreads.TryGetProperty("pageInfo", out var pageInfo))
            {
                return false;
            }

            if (pageInfo.TryGetProperty("endCursor", out var endCursorProp) &&
                endCursorProp.ValueKind == JsonValueKind.String)
            {
                endCursor = endCursorProp.GetString();
            }

            return pageInfo.TryGetProperty("hasNextPage", out var hasNextPageProp) &&
                   hasNextPageProp.ValueKind == JsonValueKind.True;
        }

        private static bool TryGetResolvedThread(JsonElement root, out string? threadId, out bool isResolved)
        {
            threadId = null;
            isResolved = false;

            if (!root.TryGetProperty("data", out var data)) return false;
            if (!data.TryGetProperty("resolveReviewThread", out var resolveReviewThread)) return false;
            if (!resolveReviewThread.TryGetProperty("thread", out var thread)) return false;

            if (thread.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
            {
                threadId = idProp.GetString();
            }

            if (thread.TryGetProperty("isResolved", out var isResolvedProp) &&
                (isResolvedProp.ValueKind == JsonValueKind.True || isResolvedProp.ValueKind == JsonValueKind.False))
            {
                isResolved = isResolvedProp.GetBoolean();
            }

            return !string.IsNullOrWhiteSpace(threadId);
        }

        private static bool TryGetUnresolvedThread(JsonElement root, out string? threadId, out bool isResolved)
        {
            threadId = null;
            isResolved = false;

            if (!root.TryGetProperty("data", out var data)) return false;
            if (!data.TryGetProperty("unresolveReviewThread", out var unresolveReviewThread)) return false;
            if (!unresolveReviewThread.TryGetProperty("thread", out var thread)) return false;

            if (thread.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
            {
                threadId = idProp.GetString();
            }

            if (thread.TryGetProperty("isResolved", out var isResolvedProp) &&
                (isResolvedProp.ValueKind == JsonValueKind.True || isResolvedProp.ValueKind == JsonValueKind.False))
            {
                isResolved = isResolvedProp.GetBoolean();
            }

            return !string.IsNullOrWhiteSpace(threadId);
        }
    }
}
