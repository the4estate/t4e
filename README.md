The Fourth Estate — M4 / News v0.4.1
- New

News DTOs (NewsDto, NewsToneDetailsDto, SourceDto) defined in Application.Abstractions
→ match news.schema.json, IL2CPP-safe.

PublishNews use case (Application.UseCases)
→ loads News from repo, applies chosen tone’s Effect[], records memory footprint, returns Headline/Short/Body.

MemoryLog (Infrastructure)
→ simple in-memory store of player footprints (e.g. “Published vic.news.demo_001 [Critical]”).

InMemoryWorld integration (Infrastructure)
→ EffectApplier forwards Effects into world state (Flags, Leads, News).

GameInstaller wiring (Presentation)
→ clean dependency injection of ports (Repo, Applier, Memory, Logger).

JsonNewsRepository (Infrastructure.Content)
→ loads authored news.json into DTOs via IContentRepository.

EditMode tests (T4E.Tests.EditMode)

Publish applies correct effects and records memory

Determinism: same input → same output

Green in Test Runner - Passed

Editor Preview Window (Tools.Editor)
→ T4E/Preview/News… lets you browse all News, preview Supportive/Neutral/Critical variants, see Headline/Short/Body and effect counts without Play Mode.

- Workflow Recap

Content: news.json authored to schema.

Repo: JsonNewsRepository loads all News.

Publish Flow:

Player chooses News ID + Tone

PublishNews.Execute loads DTO

Effects wrapped in EffectInvocation → IEffectApplier.Apply

InMemoryWorld.Apply mutates world state

MemoryLog.RecordPublishedNews logs footprint

Result returned (Headline, Short, Body)

Preview: Editor window for fast inspection.

Tests: Protect determinism & footprinting.

- Acceptance (M4 — News Masterplan)

Load News from JSON → Passed

Publish with tones → Passed

Effects applied deterministically → Passed

Headline + short + body render → Passed

Memory & dev log record → Passed

Tests green → Passed

Editor tooling for preview → Passed
=======
0.3.x
>>>>>>> 3579ebcb311b34283c9f66a365cba6f2c04b8381
