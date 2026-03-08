# Session Log: Dot-Folder Collapse — Squad Sync
**Date:** 2026-03-07T23:42:19Z

## Initiative
Issue #1: Treeview dot-prefixed folders should not be expanded by default

## Completion
- ✅ Selina: Fixed IsExpanded logic, extracted PathTreeBuilder
- ✅ Renee: Built test suite (5 passing), verified on net472
- ✅ All tests passing; branch squad/1-dot-folder-collapse ready for review

## Key Decisions
- Dot folders gate expansion at initialization: !isLeaf && !segment.StartsWith(".")
- Use PathTreeBuilder action-delegate pattern for testability
- net472 compatibility: StartsWith(string) (not char overload)
- InternalsVisibleTo attribute enables test access to PRReviewViewModel

## Next Steps
Merge squad/1-dot-folder-collapse → main, close Issue #1.
