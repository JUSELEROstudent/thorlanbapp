# Graph Report - .  (2026-07-31)

## Corpus Check
- Corpus is ~35,518 words - fits in a single context window. You may not need a graph.

## Summary
- 829 nodes · 1631 edges · 39 communities (36 shown, 3 thin omitted)
- Extraction: 96% EXTRACTED · 4% INFERRED · 0% AMBIGUOUS · INFERRED: 70 edges (avg confidence: 0.8)
- Token cost: 56,071 input · 0 output

## Community Hubs (Navigation)
- Application Core & Controllers
- Pics Calibration Feature
- Camera CRUD Feature
- Tour & DbContext (EF Core)
- Group Calibration Feature
- Increase Feature
- Microscope Feature
- EF Core Migrations
- Phase Correlation Calibration
- Auth, Login & SQLite Connection
- Project Dependencies (NuGet)
- IDS UEye Camera Service Core
- TakeTour Operations
- TIM101 Inertial Motor
- Camera Parameter Catalog
- Token Validation Handler
- Camera Settings (JSON)
- Docs & DB Schema Concepts
- Streaming Hub (SignalR)
- Focus & UEye Controllers
- IDS Peak Camera Service
- Calibration Configuration Service
- Token Validator Middleware
- NodeHomepage & Motor Devices
- Camera Discovery
- UEye SDK Value Resolution
- SQL Connection Service
- Chat Hub (SignalR)
- Camera Service Factory (Get Service)
- Lenient String JSON Converter
- Camera Discovery & Focus Service
- ITakeTour Interface
- Collage Gestor (CSV Tracking)
- Utilities (Folder/Time Helpers)
- Focus Metrics (Variance of Laplacian)
- TIM101 Device Resolution

## God Nodes (most connected - your core abstractions)
1. `GotsThorlabs.Models` - 54 edges
2. `GotsThorlabs.Interfaces` - 43 edges
3. `IdsUEyeCameraService` - 38 edges
4. `GotsThorlabs.Database.EntityRepo.Entities` - 28 edges
5. `PicsCalibration` - 26 edges
6. `ThorlabsDbContext` - 26 edges
7. `GotsThorlabs` - 24 edges
8. `TakeTour` - 20 edges
9. `GotsThorlabs.Database.EntityRepo` - 20 edges
10. `VideoCaptureCameraService` - 20 edges

## Surprising Connections (you probably didn't know these)
- `Project Guideline: Document EF Core SQLite Entity Update Workflow` --rationale_for--> `Entity + SQLite Update Guide`  [INFERRED]
  .github/copilot-instructions.md → entity-sqlite.md
- `EF Core Migrations (dotnet ef migrations add / database update)` --shares_data_with--> `__EFMigrationsHistory table`  [INFERRED]
  Database/EntityRepo/Readme.md → entity-sqlite.md
- `Program.cs db.Database.Migrate() call` --shares_data_with--> `EF Core Migrations (dotnet ef migrations add / database update)`  [INFERRED]
  entity-sqlite.md → Database/EntityRepo/Readme.md
- `GridResult` --references--> `PicsCalibration`  [EXTRACTED]
  BLL/MosaicGridCalculator.cs → Database/EntityRepo/Entities/PicsCalibration.cs
- `TakeTour` --references--> `ThorlabsDbContext`  [EXTRACTED]
  BLL/TakeTour.cs → Database/EntityRepo/ThorlabsDbContext.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **ThorlabsApp SQLite Schema Tables** — database_thorlabsapp_user_table, database_thorlabsapp_tour_table, database_thorlabsapp_stitching_table, database_thorlabsapp_image_table, database_thorlabsapp_logcalibrate_table [EXTRACTED 1.00]
- **EF Core Entity Update Workflow (Camera.DriverType case)** — entity_sqlite_camera_entity, concept_thorlabsdbcontext, concept_ef_core_migrations, entity_sqlite_program_migrate_call, entity_sqlite_efmigrationshistory_table [EXTRACTED 1.00]

## Communities (39 total, 3 thin omitted)

### Community 0 - "Application Core & Controllers"
Cohesion: 0.05
Nodes (25): Bitmap, emgu, ProcessTourData, TokenGenerator, GotsThorlabs.NodesApi, GotsThorlabs.Interfaces, GotsThorlabs.Hubs, GotsThorlabs.Controls (+17 more)

### Community 1 - "Pics Calibration Feature"
Cohesion: 0.06
Nodes (43): AxisFit, int, List, long, string, AxisFit, GridResult, MosaicGridCalculator (+35 more)

### Community 2 - "Camera CRUD Feature"
Cohesion: 0.08
Nodes (28): ActionResult, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IEnumerable (+20 more)

### Community 3 - "Tour & DbContext (EF Core)"
Cohesion: 0.08
Nodes (29): ActionResult, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IEnumerable (+21 more)

### Community 4 - "Group Calibration Feature"
Cohesion: 0.12
Nodes (21): ActionResult, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IEnumerable (+13 more)

### Community 5 - "Increase Feature"
Cohesion: 0.12
Nodes (21): ActionResult, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IEnumerable (+13 more)

### Community 6 - "Microscope Feature"
Cohesion: 0.12
Nodes (21): ActionResult, CancellationToken, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IEnumerable (+13 more)

### Community 7 - "EF Core Migrations"
Cohesion: 0.07
Nodes (17): GotsThorlabs.Migrations, Migration, MigrationBuilder, ModelBuilder, AddCalibrationTables, AddCalibrationTables, MigrationBuilder, ModelBuilder (+9 more)

### Community 8 - "Phase Correlation Calibration"
Cohesion: 0.10
Nodes (18): Mat, PhaseCorrelationDetector, PhaseCorrelationUtils, confidence, Consumes, HttpPost, IActionResult, IFormFile (+10 more)

### Community 9 - "Auth, Login & SQLite Connection"
Cohesion: 0.10
Nodes (16): Authorize, HttpGet, HttpPost, HttpPut, IActionResult, LoginController, IConfiguration, IAuthService (+8 more)

### Community 10 - "Project Dependencies (NuGet)"
Cohesion: 0.08
Nodes (25): net6.0, GotsThorlabs, AForge.Video.DirectShow (2.2.5), Dapper (2.1.28), Emgu.CV.runtime.windows (4.7.0.5276), IDSImaging.Peak.API (1.15.0.4), IDSImaging.Peak.IPL (1.17.2.4), Microsoft.AspNetCore.Authentication.Abstractions (2.2.0) (+17 more)

### Community 11 - "IDS UEye Camera Service Core"
Cohesion: 0.17
Nodes (9): CameraInformation, IntPtr, bool, int, Mat, SemaphoreSlim, string, IdsUEyeCameraService (+1 more)

### Community 12 - "TakeTour Operations"
Cohesion: 0.12
Nodes (13): decimal, Dictionary, dynamic, IAsyncEnumerable, int, KCubeInertialMotor, List, long (+5 more)

### Community 13 - "TIM101 Inertial Motor"
Cohesion: 0.16
Nodes (9): Dictionary, dynamic, IAsyncEnumerable, int, List, Mat, MotorChannels, string (+1 more)

### Community 14 - "Camera Parameter Catalog"
Cohesion: 0.14
Nodes (9): CameraParameterDescriptor, IReadOnlyList, bool, Dictionary, IReadOnlyList, Mat, object, SemaphoreSlim (+1 more)

### Community 15 - "Token Validation Handler"
Cohesion: 0.17
Nodes (13): AuthorizationHandler, AuthorizationHandlerContext, Authorizationadmin, Httpcontextentry, TokenValidationHandler, DateTime, SecurityToken, Task (+5 more)

### Community 16 - "Camera Settings (JSON)"
Cohesion: 0.17
Nodes (5): Action, Dictionary, JsonSerializerOptions, CameraSettings, VideoCapture

### Community 17 - "Docs & DB Schema Concepts"
Cohesion: 0.18
Nodes (17): dotnet ef dbcontext scaffold command, EF Core Migrations (dotnet ef migrations add / database update), ThorlabsDbContext (EF Core DbContext), Scaffold (Reverse Engineering) Guide for EF Core Entities, image table (SQLite schema), logcalibrate table (SQLite schema), stitching table (SQLite schema), tour table (SQLite schema) (+9 more)

### Community 18 - "Streaming Hub (SignalR)"
Cohesion: 0.20
Nodes (9): Exception, CancellationToken, Dictionary, ICameraService, object, Task, ResolvedCamera, StreamingHub (+1 more)

### Community 19 - "Focus & UEye Controllers"
Cohesion: 0.14
Nodes (12): ControllerBase, ActionResult, CancellationToken, HttpPost, Task, FocusController, UEyeController, CancellationToken (+4 more)

### Community 20 - "IDS Peak Camera Service"
Cohesion: 0.18
Nodes (8): Device, IDisposable, bool, Dictionary, Mat, object, SemaphoreSlim, IdsPeakCameraService

### Community 21 - "Calibration Configuration Service"
Cohesion: 0.21
Nodes (6): Mat, ConfigurationService, IConfigurationService, GotsThorlabs.Models.Configuration, GotsThorlabs.BLL.Configuration, ConfigurationDTO

### Community 22 - "Token Validator Middleware"
Cohesion: 0.20
Nodes (8): apitest.Middleware, HttpRequestMessage, TokenValidatorMiddleware, CancellationToken, DateTime, SecurityToken, Task, TokenValidationParameters

### Community 23 - "NodeHomepage & Motor Devices"
Cohesion: 0.20
Nodes (8): Decimal, KCubeInertialMotor, MotorChannels, NodeHomepage, KimFourChanelsThorlabs, KimSingleChanel, ObjCameras, ObjMovement

### Community 24 - "Camera Discovery"
Cohesion: 0.18
Nodes (6): IEnumerable, ICameraDiscoveryService, DeviceInfo, IEnumerable, IEnumerable, IEnumerable

### Community 25 - "UEye SDK Value Resolution"
Cohesion: 0.36
Nodes (3): MethodInfo, Camera, List

### Community 26 - "SQL Connection Service"
Cohesion: 0.22
Nodes (5): apitest.Services, IDbConnection, IConnectionSql, ConnectionSql, IDbConnection

### Community 27 - "Chat Hub (SignalR)"
Cohesion: 0.22
Nodes (6): Hub, CancellationToken, IAsyncEnumerable, Task, ChatHub, UpdateStatus

### Community 28 - "Camera Service Factory (Get Service)"
Cohesion: 0.22
Nodes (4): dynamic, IAsyncEnumerable, Mat, ICameraService

### Community 29 - "Lenient String JSON Converter"
Cohesion: 0.25
Nodes (6): JsonConverter, JsonSerializerOptions, LenientStringJsonConverter, Type, Utf8JsonReader, Utf8JsonWriter

### Community 30 - "Camera Discovery & Focus Service"
Cohesion: 0.28
Nodes (5): Dictionary, CameraServiceFactory, CancellationToken, Task, FocusService

### Community 31 - "ITakeTour Interface"
Cohesion: 0.25
Nodes (3): dynamic, IAsyncEnumerable, ITakeTour

### Community 32 - "Collage Gestor (CSV Tracking)"
Cohesion: 0.29
Nodes (4): int, List, string, CollageGestor

## Knowledge Gaps
- **36 isolated node(s):** `net6.0`, `AForge.Video.DirectShow (2.2.5)`, `Dapper (2.1.28)`, `Emgu.CV.runtime.windows (4.7.0.5276)`, `IDSImaging.Peak.API (1.15.0.4)` (+31 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `GotsThorlabs.Models` connect `Application Core & Controllers` to `Pics Calibration Feature`, `Camera CRUD Feature`, `Tour & DbContext (EF Core)`, `Group Calibration Feature`, `Increase Feature`, `Microscope Feature`, `Phase Correlation Calibration`, `Auth, Login & SQLite Connection`, `Camera Parameter Catalog`, `Camera Settings (JSON)`, `Focus & UEye Controllers`, `Camera Discovery`, `Lenient String JSON Converter`?**
  _High betweenness centrality (0.180) - this node is a cross-community bridge._
- **Why does `GotsThorlabs.Interfaces` connect `Application Core & Controllers` to `SQL Connection Service`, `Camera Service Factory (Get Service)`, `ITakeTour Interface`?**
  _High betweenness centrality (0.155) - this node is a cross-community bridge._
- **Why does `ThorlabsDbContext` connect `Tour & DbContext (EF Core)` to `Application Core & Controllers`, `Pics Calibration Feature`, `Camera CRUD Feature`, `Group Calibration Feature`, `Increase Feature`, `Microscope Feature`, `Auth, Login & SQLite Connection`, `TakeTour Operations`, `Streaming Hub (SignalR)`, `Chat Hub (SignalR)`, `Camera Discovery & Focus Service`?**
  _High betweenness centrality (0.150) - this node is a cross-community bridge._
- **What connects `net6.0`, `AForge.Video.DirectShow (2.2.5)`, `Dapper (2.1.28)` to the rest of the system?**
  _36 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Application Core & Controllers` be split into smaller, more focused modules?**
  _Cohesion score 0.0515406162464986 - nodes in this community are weakly interconnected._
- **Should `Pics Calibration Feature` be split into smaller, more focused modules?**
  _Cohesion score 0.0609009009009009 - nodes in this community are weakly interconnected._
- **Should `Camera CRUD Feature` be split into smaller, more focused modules?**
  _Cohesion score 0.08333333333333333 - nodes in this community are weakly interconnected._