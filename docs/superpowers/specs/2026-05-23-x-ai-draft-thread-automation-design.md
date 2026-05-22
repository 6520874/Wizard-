# X AI Draft Thread Automation Design

## Goal

Create a Codex-thread workflow that returns every four hours with a Chinese and
English X thread draft about recent AI news, then waits for explicit user
approval before posting through the user's logged-in Chrome session.

## Scope

The first version will:

- Wake the current Codex thread every four hours from the time the automation is
  created.
- Research recent AI news from external sources such as news search results,
  official company announcements, research blogs, and other public web pages.
- Decide whether the current round should focus on one strong AI development or
  summarize up to three smaller developments.
- Draft a two-post X thread with a Chinese first post and an English reply post.
- Include sources, a short rationale for the topic choice, and a posting
  recommendation in the Codex response.
- Wait for the user's explicit approval before any X posting action.
- Use the user's Chrome session to publish the approved thread on X only after
  approval.

The first version will not:

- Auto-post without approval.
- Use X Trending topics as the automatic content source.
- Maintain a local article database, posting history database, or long-term
  deduplication store.
- Create an X API application, manage X API credentials, or publish through the
  X API.
- Bypass X login, CAPTCHA, rate-limit, or other interactive safety checks.

## User Workflow

1. A heartbeat automation wakes this Codex thread every four hours.
2. The automation researches recent external AI news and identifies the most
   useful posting angle for the current round.
3. The thread receives a draft package containing:
   - Chinese first-post draft.
   - English reply-post draft.
   - Source list with links or citations.
   - Brief explanation of why this item is worth posting now.
   - A recommendation to post, revise, skip, or wait for confirmation.
4. The user replies with a short command:
   - `发` to publish the current approved draft.
   - `改：...` to request edits before publication.
   - `跳过` to skip the current round.
   - `停` to stop future draft wakeups.
5. If the user replies `发`, Codex uses Chrome on `https://x.com/home` to post
   the Chinese first post and then post the English text as a reply.

## Architecture

The workflow has six responsibilities.

### Heartbeat Scheduler

A Codex heartbeat automation is attached to the current thread. Its schedule is
four-hour repetition from the creation time. Its prompt describes draft
generation only; it must not instruct the heartbeat job to publish on X.

### External News Research

Each heartbeat run searches for recent AI developments outside X Trending. It
prefers primary sources for product launches, policy changes, research releases,
and company announcements, and may use reputable reporting to discover or
corroborate candidate topics.

### Topic Selection

The run chooses its format dynamically:

- Use a single-topic draft when one development is timely, substantial, and
  sufficiently supported by sources.
- Use a brief roundup draft when no one development dominates and up to three
  smaller recent items produce a useful digest.
- Recommend skipping or waiting when sources are weak, conflicting, or not
  timely enough to justify a post.

### Draft Generation

The draft generator writes a compact X thread:

- Post 1 is Chinese.
- Post 2 is an English reply.
- Each post should stay concise enough for X posting without silently expanding
  into a long thread.
- The two posts should share the same factual core while reading naturally in
  their own language.
- Claims should stay tied to the cited or linked sources collected in the run.

### Approval Gate

The Codex response is the review surface. The workflow must not navigate to X
for posting or press any X publish control unless the user gives an explicit
approval for the current draft after it is shown in the thread.

### Chrome Publisher

After approval, Codex uses the user's logged-in Chrome session to open X Home,
enter the Chinese first post, publish it, open the reply composer for that post,
and publish the English reply. If Chrome cannot reach a usable X session, the
publisher stops and reports the blocker.

## Data Flow

1. Heartbeat wakes the current Codex thread.
2. Research collects current source material and timestamps.
3. Topic selection chooses single-topic, roundup, or skip recommendation.
4. Draft generation produces the Chinese and English texts plus review context.
5. User approves, requests revision, skips, or stops the automation.
6. Only an approval continues into Chrome posting.

## Error Handling

- If the research round cannot find a well-supported timely topic, it should say
  that the round is not recommended for posting instead of inventing urgency.
- If important sources conflict or a claim is still uncertain, the draft package
  should mark that uncertainty and default to waiting or revision.
- If the text is too long for a compact two-post thread, the drafting step
  should compress it before review.
- If Chrome is not logged into X, X requires user interaction, or page controls
  are unavailable, publication stops and the user receives the exact blocker.
- If the user requests edits, the approval state resets around the revised text;
  the revised text must be shown before publication.

## Platform Constraints

X permits some automated informational posting, but its automation rules also
restrict automated posting around trending topics. This design therefore uses
external AI news sources for the draft workflow and keeps a human approval gate
before posting.

## Validation

The first version should be validated by checking that:

- The Codex heartbeat is attached to this thread and repeats every four hours.
- A heartbeat prompt asks for sources, a draft package, and a recommendation
  without asking the run to auto-publish.
- A sample heartbeat result includes Chinese first-post text, English reply
  text, source context, rationale, and a recommendation.
- No posting action is attempted before the user explicitly approves the shown
  draft.
- An approved Chrome posting flow creates a first post and reply, or stops
  cleanly on login, CAPTCHA, X blocking, or changed page controls.

## Future Extensions

Later iterations can add source allowlists, article and draft deduplication,
local history, per-topic style presets, and an X API publisher if the workflow
needs more durable background operation than Chrome-assisted review posting.
