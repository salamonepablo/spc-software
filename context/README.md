# Context Folder

This folder contains session context files to help AI assistants (Claude, ChatGPT, etc.) quickly resume work after session interruptions or context loss.

## Files

| File | Purpose |
|------|---------|
| `current_session.md` | Active session state - always read this first |
| `session_YYYY-MM-DD.md` | Daily backup of session context |

## Usage

**For AI assistants:**
1. Read `current_session.md` to understand current state
2. Check recent `session_*.md` files for historical context
3. Update `current_session.md` after significant work
4. Create/update daily file after commits

**For Pablo:**
- If session is lost, point new AI to this folder
- Review daily files to track progress over time
