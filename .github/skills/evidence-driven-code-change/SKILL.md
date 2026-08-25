---
name: evidence-driven-code-change
description: 'Use for debugging, implementing, or repairing code changes when the controlling path must be identified before editing. Guides targeted repository exploration, falsifiable hypotheses, minimal edits, focused validation, and evidence-based iteration.'
argument-hint: 'Describe the requested behavior, failing symptom, or target file and expected outcome.'
user-invocable: true
---

# Evidence-Driven Code Change

## When to Use

Use this skill for a bug fix, behavior change, or small implementation task where the repository should be inspected before code is changed. It is especially useful when several nearby files could plausibly own the behavior or when a focused test or command can quickly confirm the diagnosis.

## Outcome

Produce a minimal, testable change that addresses the controlling code path, preserves unrelated work, and reports the validation performed and any remaining risk.

## Procedure

1. **Anchor the investigation.** Start from the most concrete available anchor: a named file, symbol, failing behavior, command, test, or nearby implementation surface. Search narrowly and read only enough surrounding code to understand the path.
2. **Find the decision point.** If the anchor only wires, forwards, registers, or displays the behavior, follow one nearby hop to the code that computes, mutates, or controls it. Prefer an existing helper, neighboring test, or call site over broad repository mapping.
3. **State a local hypothesis.** Before editing, write down one falsifiable explanation for the behavior and one cheap check that could disconfirm it. Identify the smallest edit that will test the hypothesis.
4. **Resolve only necessary ambiguity.** If multiple local paths remain plausible, make one targeted read of the nearest abstraction boundary or dependent call site. Then choose the best-supported path. Do not continue searching merely to gain confidence.
5. **Edit the smallest slice.** Preserve existing APIs, style, and unrelated user changes. Add a comment only when a non-obvious block would otherwise require tedious parsing. Avoid opportunistic refactors.
6. **Validate immediately.** After the first substantive edit, run the cheapest behavior-scoped check available. Prefer, in order: the failing check, a narrow test, or a narrow compile, lint, or typecheck command. Do not broaden exploration before this check.
7. **Iterate from evidence.**
   - If validation supports the hypothesis but reveals a local defect, repair the same slice and rerun the same check.
   - If validation falsifies the hypothesis, step one nearby hop toward the code that directly controls the behavior, then reassess.
   - If validation is ambiguous, perform one nearby disambiguating read or inspect a neighboring test/call site, then choose local repair or the one-hop move.
   - If validation succeeds but an adjacent edit is required, make that edit and rerun focused validation.
8. **Finish with executable evidence.** Run at least one post-edit executable validation whenever the environment provides one. Use a diff-only check only when no focused command is available or commands cannot run.
9. **Report clearly.** Summarize the behavior changed, link the touched files, state the validation command and result, and mention unrelated failures, unavailable checks, or residual risk without claiming more certainty than the evidence supports.

## Decision Rules

- Prefer the owning abstraction over a caller or presentation layer.
- Prefer a cheap discriminating check over a broad test suite at the start.
- Treat a validation failure as information about the hypothesis, not as a reason to patch unrelated code.
- Do not revert changes that were not made during this task.
- Stop once the requested behavior is covered and focused validation passes; do not expand scope without evidence.

## Completion Checklist

- [ ] The controlling code path was identified.
- [ ] A falsifiable hypothesis and discriminating check were established before editing.
- [ ] The change is minimal and localized.
- [ ] Focused executable validation was run after editing.
- [ ] Any remaining test gap or unrelated failure is documented.
