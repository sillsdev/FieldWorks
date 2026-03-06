# GitHub Copilot For Jira Setup

FieldWorks uses authenticated Jira Data Center access. Jira issues are not anonymously public, but some issues may be designated as safe for agent use through the approved service-user/API policy.

The rule for this repository is:

- agent access to Jira must go through the approved service user,
- the service user's API scope must determine which issues are exposed,
- personal Jira credentials must not be used for automated Copilot setup or workflow runs.

## Repository Guidance

The reusable workflow at `.github/workflows/copilot-setup-steps.yml` is intended to stay safe for Jira-triggered agent runs because it:

- uses least-privilege repository permissions,
- avoids installer/signing/release secrets,
- allows Jira credentials only when they are supplied as part of the approved service-user setup,
- requires `JIRA_URL` when Jira API credentials are present.

What this workflow does not enforce:

- whether an individual Jira issue is approved for agent visibility,
- which Jira projects are in scope,
- the service user's effective permissions.

Those controls belong in the Jira-side API/service-user policy.

## Required Secrets

For the FieldWorks Jira Data Center setup, the normal configuration is:

- `JIRA_URL`
- `JIRA_PAT_TOKEN`

If your integration layer uses different variable names, keep them mapped at the caller workflow or environment level and avoid changing this reusable workflow unless the contract changes.

Guidance:

- Prefer environment or organization secrets over ad hoc per-workflow values.
- Use a dedicated service-user PAT or API token.
- Do not use a personal admin token.
- Scope the service account so the API only exposes issues marked for agent/public handling.

## Optional Variants

Other helper tooling in this repository may still support:

- Jira Cloud: `JIRA_URL`, `JIRA_USERNAME`, `JIRA_API_TOKEN`
- Jira Data Center: `JIRA_URL`, `JIRA_PAT_TOKEN`

For FieldWorks, the Data Center service-user path is the intended default.

## Recommended Admin Setup

1. Create a dedicated Jira service user for Copilot/agent access.
2. Grant that service user only the minimum permissions needed to read issues approved for agent use.
3. Enforce issue visibility through your API layer or Jira-side policy, rather than assuming anonymous public access.
4. Store `JIRA_URL` and `JIRA_PAT_TOKEN` as managed secrets at the environment or organization level.
5. Keep direct browsing of Jira URLs out of agent tooling unless it is going through the approved authenticated integration path.