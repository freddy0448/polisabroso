# Issue tracker: GitHub

Issues and specs live in GitHub Issues for `freddy0448/polisabroso`.
Use the `gh` CLI from the repository root.

## Conventions

- Verify the target before creating issues:
  `gh repo view --json nameWithOwner`.
  It must be `freddy0448/polisabroso`.
- Create: `gh issue create --title "..." --body-file <file>`.
  Write multiline bodies to a temporary UTF-8 file.
- Read:
  `gh issue view <number> --json number,title,body,labels,comments`.
- List:
  `gh issue list --state open --json number,title,body,labels`.
  Add label filters as needed.
- Comment: `gh issue comment <number> --body-file <file>`.
- Label:
  `gh issue edit <number> --add-label "..." --remove-label "..."`.
- Close: post the explanation as a comment, then run
  `gh issue close <number>`. Comment separately even if already closed.

Use native GitHub sub-issues and issue dependencies when available.
Otherwise, put `Part of #<parent>` and `Blocked by: #<number>` at the
top of the child issue body, and link children in the parent's task list.
A ticket is unblocked when every blocker is closed.

## Pull requests as a triage surface

**PRs as a request surface: no.**

## Skill instructions

When a skill says "publish to the issue tracker", create a GitHub issue.
When a skill says "fetch the relevant ticket", read the GitHub issue.
