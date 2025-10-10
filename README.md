The Fourth Estate — v0.4.2


Leads fully playable and verifiable:

Spawn a lead → collect evidence → Expose → apply effects like News.

Deterministic, world-saveable state model (no Unity deps in Domain/UseCases).

News are now locked to sources and a Source Credibility Calculator estimates agency “circulation & credibility”.

Project layout 
Assets/The Fourth Estate
├── Application
│   ├── Abstractions
│   │   ├── Dtos/        # NewsDto, LeadDto, SourceDto, etc.
│   │   └── Ports/       # ILeadRepository, ILeadProgressStore, ...
│   └── UseCases
│       ├── Leads/       # CollectEvidence, ExposeLead
│       ├── News/        # PublishNews
│       └── Time/        # EffectApplier, timeline helpers
├── Domain
│   ├── CET/             # Effects, triggers, trigger context
│   └── Core/
│       ├── Leads/       # LeadProgress (runtime), LeadProgressState (save)
│       └── ...          # GameDate, WorldSnapshot, etc.
├── Infrastructure
│   ├── Content/         # JSON loaders (news, sources, leads ...)
│   └── Systems/         # Adapters: InMemoryWorld, AppLogger, Random, Clock
├── Bootstrap/Installers # CoreContainer + installers (manual DI)
└── Tools.Editor         # Validators + Preview windows (News, Leads)
