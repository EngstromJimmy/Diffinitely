namespace Diffinitely.Models
{
 internal class ChangedFileInfo
 {
 public string Path { get; set; }
 public string PreviousPath { get; set; } // for renames
 public ChangeKind Kind { get; set; }
 }

 internal enum ChangeKind
 {
 Added,
 Modified,
 Deleted,
 Renamed,
 }
}
