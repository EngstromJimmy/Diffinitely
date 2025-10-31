using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Diffinitely.Services
{
 // Lightweight repo info extraction without LibGit2Sharp.
 internal class GitRepositoryService
 {
 public string FindRepoRoot(string startPath)
 {
 var dir = new DirectoryInfo(startPath);
 while (dir != null)
 {
 if (Directory.Exists(Path.Combine(dir.FullName, ".git")))
 return dir.FullName;
 dir = dir.Parent;
 }
 throw new InvalidOperationException("Git root not found");
 }

 public string GetCurrentBranch(string repoRoot)
 {
 var headFile = Path.Combine(repoRoot, ".git", "HEAD");
 if (!File.Exists(headFile)) return "";
 var text = File.ReadAllText(headFile).Trim();
 // format: ref: refs/heads/branch
 if (text.StartsWith("ref:"))
 {
 var parts = text.Split(' ');
 var refPath = parts[1];
 return refPath.Substring(refPath.LastIndexOf('/') +1);
 }
 return text; // detached hash
 }

 public bool TryGetRemoteOriginUrl(string repoRoot, out string owner, out string name)
 {
 owner = name = string.Empty;
 var config = Path.Combine(repoRoot, ".git", "config");
 if (!File.Exists(config)) return false;
 var lines = File.ReadAllLines(config);
 bool inOrigin = false;
 foreach (var l in lines)
 {
 var line = l.Trim();
 if (line.StartsWith("[remote \"origin\"]", StringComparison.OrdinalIgnoreCase))
 { inOrigin = true; continue; }
 if (line.StartsWith("[remote ")) inOrigin = false;
 if (inOrigin && line.StartsWith("url = "))
 {
 var url = line.Substring(6).Trim();
 // Handle https://github.com/owner/repo(.git) or git@github.com:owner/repo.git
 var m = Regex.Match(url, @"github\.com[:/](?<owner>[A-Za-z0-9_.-]+)/(?<repo>[A-Za-z0-9_.-]+)(?:\.git)?$", RegexOptions.IgnoreCase);
 if (m.Success)
 {
 owner = m.Groups["owner"].Value;
 name = m.Groups["repo"].Value;
 return true;
 }
 }
 }
 return false;
 }
 }
}
