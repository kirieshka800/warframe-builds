# Где методы в диаграмме классов

Откройте [03-class.drawio](diagrams/03-class.drawio) в diagrams.net и выберите **вторую вкладку снизу: «2 · Методы сервисов и контроллеров»**.

[Посмотреть вторую страницу без редактора](previews/03-class-2.svg).

На первой странице показаны сущности EF Core: Build, BuildMod, Mod, Warframe, Weapon, BuildComment, Guide и GearItem. В их текущих объявлениях нет собственных методов: они содержат свойства и навигационные ссылки. Поэтому в нижних секциях стоит «—». Добавлять туда вымышленные `Build.Save()` / `Mod.Upgrade()` было бы неправильно при требовании отразить реальный код.

C#-свойства `get; set;` представлены как UML-атрибуты. Их сгенерированные аксессоры `get_Title()`/`set_Title()` обычно не перечисляют как отдельные операции. Вычисляемые свойства `TargetName` и `IsWeapon` также остаются атрибутами с пометкой `{readOnly}`.

На второй странице нижняя секция каждого класса содержит его реальные методы. Например:

| Класс | Реальные операции |
|---|---|
| BuildsController | Create, Index, Details, Edit, AddMod, SetSlotPolarity, Delete, Duplicate, AddComment, Export, Import |
| AutoBuildController | Index, Preview, Proposal, Confirm, Cancel |
| GuidesController | Index, Details, Create, Edit, Delete |
| BuildCalculator | `Calculate(build: Build): BuildStatistics`; `ModCost(mod: Mod, rank: int, polarity: string, specialSlot: bool = false): int` |
| ModCompatibility | `CanEquip(build: Build, mod: Mod, slot: int): bool`; `ValidSlots(build: Build): int[]`; `IsMelee(build: Build): bool`; `Family(mod: Mod): string` |
| AutoBuildPlanner | `Goals(isWeapon: bool): IReadOnlyDictionary<string,string>`; `Plan(target: Build, catalog: IReadOnlyList<Mod>, goal: string, maxFormas: int = 0): AutoBuildPlan` |
| AutoBuildProposalStore | `Add(owner: string, plan: AutoBuildPlan, fingerprint: string): string`; `Get(token: string?): Proposal?`; `Dispose(): void` |
| WarframeApiService | SyncModsAsync, SyncWarframesAsync, SyncWeaponsAsync, SyncGearCategoryAsync |
| ApplicationDbContext | `OnModelCreating(m: ModelBuilder): void` — protected override |

В сигнатурах контроллеров опущены некоторые параметры (`...`), чтобы не перегружать рисунок. Названия методов и основные возвращаемые типы сохранены. Статические методы помечены `{static}`. У framework-классов показаны только контекст и роль, полный список их унаследованных методов не развёрнут.

Такое распределение соответствует архитектуре приложения: данные хранятся в моделях; HTTP-операции и проверка прав — в контроллерах; расчёт и подбор — в статических сервисах; сохранение — через EF Core. Переносить CRUD-методы в сущности только ради диаграммы означало бы изображать другую программу.
