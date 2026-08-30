# DummyDll 生成实施计划

- 日期：2026-08-30
- 设计依据：[DummyDll 生成方案（Il2CppDumper 逻辑迁移）](dummydll-il2cppdumper-migration.md)
- 目标：在保留现有 HSR metadata 解析链的前提下，复用 Il2CppDumper 的 Mono.Cecil 生成逻辑，分三个阶段实现 DummyDll 输出，并建立适用于 100 MB 以上 `dump.cs` 的覆盖验证流程。

---

## 0. 实施状态

### 第一阶段：已完成（2026-08-30）

已完成内容：

- 增加统一 CLI、版本配置、metadata magic、ImageBase 和结构步长配置。
- 保持旧的单位置参数调用和原 `dump.cs`、`stringliterals.json` 默认输出行为。
- 加入 `Mono.Cecil 0.11.4`。
- 嵌入并校验 `Il2CppDummyDll.dll` 模板。
- 实现 resolver、空白程序集生成、临时目录验证和目录替换导出流程。
- `--dummy-dll` 能为全部 142 个 metadata image 生成空白 DLL，并额外输出模板 DLL，共 143 个。
- 增加第一阶段单元测试和真实样本集成验证。

源码复用决策：第一阶段只需要模板 DLL、resolver 语义和少量生成行为，因此暂不引入 submodule，也不迁入完整 `DummyAssemblyGenerator`。第二阶段开始前重新评估完整成员生成的依赖闭包；只有依赖明显扩散时才采用固定 commit 的 submodule。

验证结果：

- 第一阶段单元测试：14/14 通过。
- 142 个游戏 DLL 均可被 Mono.Cecil 重新加载，且没有业务类型。
- 默认配置与显式 `OSPRODWin4.5.0`/magic/ImageBase 配置生成的 143 个文件名完全一致。
- 新旧 `dump.cs` 均为 136,224,400 字节且 SHA-256 一致。
- 新旧 `stringliterals.json` 均为 11,735,776 字节且 SHA-256 一致。
- 错误 metadata magic 会在解析前给出明确诊断并以非零状态退出。

CLR/Unity 外部依赖优化仍保持可选，第一阶段未实现，不影响进入第二阶段。

---

## 1. 总体范围和约束

1. 保留现有 HSR `global-metadata.dat`、`startup-metadata.dat` 和 `GameAssembly.dll` 解析逻辑，不重建标准 IL2CPP metadata。
2. 只迁移或复用 Il2CppDumper 的 DummyDll 生成层，不调用 `Il2CppDumper.exe`。
3. DummyDll 是新增产物，不替代现有 `dump.cs` 和 `stringliterals.json`。
4. 最终保持 Mono.Cecil 的生成遍次：程序集和类型 → 嵌套/泛型/继承/接口 → 委托骨架 → 字段 → 方法 → 属性 → 事件。
5. 第一轮实现不恢复 attribute blob，不引入 Registration 扫描、IDA/Ghidra 脚本等与 DummyDll 无直接关系的功能。
6. Il2CppDumper fork 必须固定到经过验证的 commit。当前本地候选提交为 `7a1bb2ec74adaad8606cb36bd86d551f2cd9f78a`，正式编码前应在仓库中记录最终选定值。
7. 未提供游戏/metadata 版本时继续使用当前 `OSPRODWin4.5.0` 默认配置；未知版本且覆盖参数不足时必须停止，不得静默生成可能损坏的 DLL。

---

## 2. 第一阶段（已完成）：搭建框架、参数和空白 DummyDll 输出

### 2.1 阶段目标

新增命令行参数能够被正确识别，解析配置集中生效，并能为每个 image 生成可被 Mono.Cecil、dnSpy 或 ILSpy 打开的空白程序集。

本阶段只验证完整调用链：

```text
命令行参数
    → 版本和解析配置
    → HSR metadata 初始化
    → DummyDll 生成器入口
    → 创建空白 assembly/module
    → 写入 DummyDll/*.dll
    → Cecil 重新加载验证
```

### 2.2 评估并确定 Il2CppDumper 复用方式

1. 对以下文件进行实际依赖闭包分析：

   - `DummyAssemblyGenerator.cs`
   - `DummyAssemblyExporter.cs`
   - `MyAssemblyResolver.cs`
   - `Il2CppDummyDll.dll`

2. 按以下标准选择迁移方式：

   - 如果只需生成器、导出器、resolver 和少量简单模型，则在本仓库保留裁剪后的生成层。
   - 如果为了消除依赖必须持续引入 `Il2CppExecutor`、官方 `Metadata`、二进制扫描器等大范围代码，则使用 `extern/Il2CppDumper` submodule。
   - 不允许为了兼容生成器而让 HSR metadata 转回官方 metadata 格式。

3. 固定最终使用的 commit，并记录：

   - 上游仓库和 commit。
   - 迁入或修改的文件列表。
   - 本仓库适配修改。
   - MIT 许可证来源。

4. 如果采用 submodule：

   - submodule 固定 commit，不跟随上游分支浮动。
   - 构建文档和 CI 明确执行递归拉取或初始化。
   - 主仓库只依赖一个稳定的生成层调用入口，不直接依赖大量内部实现类。

### 2.3 添加项目依赖和外部资源

1. 在主项目中添加 `Mono.Cecil 0.11.4`，与目标 Il2CppDumper 版本保持一致。
2. 增加测试项目，用于放置参数、类型转换、生成器及小型集成测试。
3. 将 `Il2CppDummyDll.dll` 作为嵌入资源加入项目：

   - 运行时通过程序集资源流读取。
   - 不依赖开发机上的绝对路径。
   - 发布后仍能正常读取。
   - 启动时检查模板是否包含 `AddressAttribute`、`FieldOffsetAttribute`、`MetadataOffsetAttribute` 和 `TokenAttribute` 等预期类型。

4. 建议的目录结构：

```text
HSRGlobalMetadata/
├── Configuration/
│   ├── CommandLineOptions.cs
│   ├── GenerationOptions.cs
│   └── VersionProfile.cs
├── DummyDll/
│   ├── Adapters/
│   ├── Generation/
│   ├── Resources/
│   └── Validation/
└── Output/

HSRGlobalMetadata.Tests/
├── Configuration/
├── DummyDll/
└── Fixtures/
```

目录名称可以根据现有编码风格调整，但配置、HSR 适配和 Cecil 生成逻辑应保持职责分离。

### 2.4 建立统一配置对象

1. 增加统一的解析和生成选项对象，至少包含：

   - 游戏目录。
   - 输出根目录。
   - 是否生成 DummyDll。
   - 游戏/metadata 版本。
   - metadata magic。
   - ImageBase。
   - 结构步长和版本相关解密参数。
   - 是否生成现有 `dump.cs`、`stringliterals.json`。
   - 是否启用严格验证。

2. 建议支持以下调用方式：

```text
HSRGlobalMetadata <game-dir> --dummy-dll
HSRGlobalMetadata <game-dir> --dummy-dll --output <directory>
HSRGlobalMetadata <game-dir> --dummy-dll --version OSPRODWin4.5.0
HSRGlobalMetadata <game-dir> --dummy-dll --image-base 0x180000000
```

3. 参数处理规则：

   - 保留当前只有游戏目录的位置参数调用方式。
   - 未指定 `--dummy-dll` 时，现有行为不变。
   - 数值参数统一支持十进制和 `0x` 十六进制。
   - 先加载版本配置，再应用命令行单项覆盖。
   - 集中检查路径、数值范围、配置冲突和未知版本。
   - 启动日志打印最终生效的版本、ImageBase、magic、结构步长和输出目录。

4. 将当前分散的可变常量逐步改为从统一配置读取，包括但不限于：

   - `Il2CppType.ImageBase`
   - `CodeRegistration` 中的 ImageBase
   - `MetadataRegistration` 中的 ImageBase
   - `MetadataTables` 中与 ImageBase 相关的计算
   - `DumpWriter` 的 RVA 计算
   - 类型、image、method、generic parameter 等结构步长

5. 解析代码不得直接读取命令行参数；所有参数只能通过已经校验的配置对象向下传递。

### 2.5 定义 HSR 生成器适配接口

1. 建立只读适配接口，使生成层不直接依赖 HSR 文件格式细节。
2. 第一阶段至少暴露：

   - image 数量、名称、typeStart 和 typeCount。
   - 程序集名称生成规则。
   - 生成配置和输出路径。

3. 后续字段、方法、属性、事件和类型引用查询入口可以先定义接口，再在第二、第三阶段逐步实现。
4. 适配层以 metadata 原始下标作为稳定 ID，不以名称作为唯一标识。

### 2.6 可选优化：将 CLR 和 Unity 类型作为外部依赖

该优化不作为第一阶段的强制验收项。编码前先做一次小范围复杂度评估；只有在依赖来源稳定、解析规则简单且能明显减少生成量时，才在第一或第二阶段提前实现，否则推迟到完整 DummyDll 基线通过之后。

1. 优化目标：

   - CLR 基础程序集和 Unity 公共库不再重复生成 DummyDll。
   - 生成的游戏程序集通过 `AssemblyNameReference` 和外部作用域的 `TypeReference` 引用这些已知程序集。
   - 生成器只处理 HSR/游戏自身定义，减少类型数量、生成时间和已知库的验证成本。

2. 候选外部依赖：

   - CLR：`mscorlib`、`System.*`、`netstandard` 或目标运行时对应的 reference assemblies。
   - Unity：`UnityEngine.*`、`Unity.*` 等与样本版本匹配的公共程序集。

3. 依赖来源：

   - CLR 优先使用明确版本的 reference assembly，而不是本机运行时实现 DLL。
   - Unity 依赖由用户通过引用目录显式提供，或从经过验证的游戏/Unity Managed 目录发现。
   - 不把 Microsoft 或 Unity 的二进制直接复制、嵌入或提交到本仓库。

4. 可以预留以下选项；只有确定实现该优化时才正式开放：

```text
--reference-dir <directory>
--unity-reference-dir <directory>
--prefer-external-framework-assemblies
```

5. resolver 的建议查找顺序：

   1. 用户显式指定的 reference 目录。
   2. 已验证的 CLR reference pack。
   3. 已验证的 Unity reference 目录。
   4. 本次生成的 DummyDll 和 `Il2CppDummyDll.dll`。

6. 只有同时满足以下条件时，才跳过对应 image 的本地生成：

   - 外部 assembly identity 与期望名称兼容。
   - 本次实际引用的类型全部能在外部程序集内找到。
   - 泛型 arity、嵌套关系和公开类型种类与 metadata 期望一致。
   - 没有同名程序集或同名类型歧义。

7. 外部依赖缺失、版本不兼容或类型不完整时：

   - 默认回退为生成本地 DummyDll，不中断基础功能。
   - 严格模式可以将不兼容视为错误。
   - 日志和验证报告必须列出哪些程序集被外部化、哪些发生了回退。

8. 前期实现评估标准：

   - 如果只需要增加 reference 目录、assembly identity 校验和 resolver 注册，则可以前置实现。
   - 如果必须处理复杂的 Unity 版本映射、类型转发、程序集重定向或大量缺失 API，则暂不实现，避免拖慢三个核心阶段。

### 2.7 实现空白程序集生成和导出

1. 从嵌入资源加载 `Il2CppDummyDll.dll`。
2. 为每个需要本地生成的 `MetadataCache.Images` 项创建一个 Cecil assembly/module：

   - 程序集名使用 `Path.GetFileNameWithoutExtension(image.Name)`。
   - 初始版本使用 `0.0.0.0`。
   - module kind 为 DLL。
   - 所有程序集注册到 `MyAssemblyResolver`。

   未启用外部依赖优化时，全部 image 都需要本地生成；启用后，只有通过 2.6 节完整校验的 CLR/Unity image 可以由已注册的外部 assembly 替代。

3. 第一阶段不向游戏程序集写入类型，仅保留 Cecil 自动创建的必要模块结构。
4. 默认输出到 `<output-root>/DummyDll/`。
5. 导出器不得调用 `Directory.SetCurrentDirectory`。
6. 对输出文件名进行合法化、空名称和重复名称检查。
7. 每个 DLL 写盘后立即通过 Mono.Cecil 重新打开，验证 PE/CLI 结构有效。
8. 生成失败时不要留下会被误认为完整结果的 DLL；可以先写临时文件，验证成功后再替换最终文件。

### 2.8 第一阶段测试

单元测试覆盖：

1. 位置参数和新增参数组合。
2. 十进制、十六进制数值解析。
3. 默认配置、版本配置和单项覆盖的优先级。
4. 未知参数、重复参数、非法范围和未知版本。
5. 未指定 `--dummy-dll` 时的兼容行为。
6. 模板资源能从构建和发布产物中读取。
7. assembly resolver 能解析模板 DLL 和两个互相引用的空程序集。
8. 如果启用外部依赖优化，验证 resolver 优先级、identity 校验、缺失依赖回退和“不复制依赖二进制”约束。

样本集成测试：

```text
dotnet run -c Release -- sample/HSR --dummy-dll
```

### 2.9 第一阶段验收标准

- 旧命令仍可正常生成 `dump.cs` 和 `stringliterals.json`。
- 新命令能生成 `DummyDll/Il2CppDummyDll.dll` 和每个需要本地生成的 image 对应的空白 DLL。
- “本地输出程序集 + 已验证的外部程序集映射”能够覆盖全部 `MetadataCache.Images`；未启用外部依赖优化时，输出程序集数量和名称必须与 images 一致。
- 所有输出均可被 Mono.Cecil 重新打开。
- 启动日志能完整反映最终生效的版本和关键常量。
- 外部依赖优化可以不在本阶段启用；未启用时不得阻塞空白 DummyDll 输出。
- 尚不要求输出游戏类型和成员。

---

## 3. 第二阶段：生成非泛型类型、委托、属性和字段

### 3.1 阶段目标

搭建正式的多遍生成流程，完整产出所有需要本地生成的非泛型类、结构、枚举、接口和委托，并生成字段、字段常量、字段偏移及属性定义。委托必须在字段类型解析前建立，避免涉及委托的字段因为目标类型不存在而退化或生成失败。被外部化的 CLR/Unity 类型不重复生成，但所有对它们的引用必须能够被 resolver 解析。

原版生成器必须先创建 `MethodDefinition`，属性才能正确绑定 getter/setter。因此本阶段会建立属性所需的最小访问器骨架。对于同时包含 getter 和 setter 的属性，统一假设其为自动属性，并生成或复用 backing field 以及对应的最小 IL；第三阶段再补齐其他方法签名、方法体和地址信息。

### 3.2 建立稳定的 metadata 适配层

1. 为以下 HSR 结构提供只读访问模型：

   - `Il2CppImageDefinition`
   - `Il2CppTypeDefinition`
   - `Il2CppFieldDefinition`
   - `Il2CppPropertyDefinition`
   - `Il2CppMethodDefinition`
   - interface 和 nested type 索引表

2. 建立生成期映射：

   - `typeDefIndex → TypeDefinition`
   - `fieldIndex → FieldDefinition`
   - `methodIndex → MethodDefinition`
   - `propertyIndex → PropertyDefinition`

3. 所有索引访问先校验范围，错误信息至少包含：

   - image 名称或下标。
   - type definition 下标。
   - member 下标。
   - 原始表范围。

4. 适配层负责解释 HSR 定制字段；Cecil 生成器不得直接读取 metadata 字节数组。

### 3.3 第一遍：创建类型骨架

1. 先为所有 image 建立 assembly/module。
2. 为需要本地生成的类型建立 Cecil `TypeDefinition`：

   - 名称和命名空间。
   - `TypeAttributes`。
   - class、struct、enum、interface 分类。
   - 暂不恢复 TokenAttribute。
   - 暂不恢复 custom attributes。

3. 完成嵌套关系：

   - 顶层类型加入 `ModuleDefinition.Types`。
   - 嵌套类型加入父类型的 `NestedTypes`。
   - 嵌套类型不得同时作为顶层类型写入。

4. 对嵌套在泛型父类型下的非泛型类型，为父类型建立必要的类型骨架，避免破坏嵌套结构；泛型参数和约束留到第三阶段完成。
5. 保留 `typeDefIndex → TypeDefinition` 映射，后续遍次只引用已创建的 Cecil 类型节点。
6. 在字段遍历前识别所有直接或间接继承 `System.MulticastDelegate` 的类型，并将其加入第二阶段的委托生成集合。

### 3.4 第二遍：补充基础类型关系

1. 先实现最小 `TypeReference` 解析集：

   - IL2CPP primitive → Cecil `TypeSystem`。
   - CLASS/VALUETYPE → 已创建的 `TypeDefinition`。
   - 同程序集类型引用。
   - 跨程序集类型引用和 `ImportReference`。

2. 添加以下关系：

   - parent/base type。
   - interface implementation。
   - `System.ValueType`、`System.Enum` 等基础关系。
   - struct 和 enum 的值类型语义。

3. 本阶段对成员类型中的 GENERICINST 使用以下临时规则：

   - CLR 集合泛型保留为真实的 `GenericInstanceType`，不能退化为 `System.Object`。
   - CLR 集合通过“程序集 identity + 完整命名空间 + 类型名 + 泛型 arity”识别，不得只按短类型名猜测。
   - 初始白名单至少考虑 `System.Collections.Generic` 下实际在样本中出现的 `List<>`、`Dictionary<,>`、`HashSet<>`、`Queue<>`、`Stack<>`、`IEnumerable<>`、`ICollection<>`、`IList<>` 和 `IDictionary<,>`；最终集合以样本扫描结果为准。
   - 集合类型优先指向 2.6 节所述外部 CLR reference assembly；未启用外部依赖优化时，指向本次生成或适配的 CLR 类型骨架。
   - 集合的类型实参递归解析；某个实参本身是暂不支持的自定义泛型时，只将该实参替换为 `System.Object`，保留外层集合结构。
   - 非 CLR 集合的泛型成员类型在第二阶段统一替换为 `System.Object`，并记录原始泛型定义、实参和成员位置。
   - 泛型委托作为成员类型时也遵循该临时规则；非泛型委托必须生成真实引用。

4. 本阶段遇到 ARRAY、PTR、VAR/MVAR、byref 等其他复杂类型引用时：

   - 仍创建对应字段或属性节点，保证成员存在性覆盖。
   - 暂时使用明确标记的 `System.Object` 占位引用。
   - 记录结构化诊断，包含原始 type code、Data、attrs 和使用位置。
   - 第三阶段完成后，严格模式下不允许遗留占位类型。

5. 禁止使用 `Il2CppType.Name()` 返回的字符串直接拼装 Cecil 类型；即使本阶段只支持基础类型，也应通过统一的 `TypeReference` resolver 创建节点。

### 3.5 生成委托类型

委托类型必须在字段生成之前完成，以便字段、属性和其他类型可以引用真实的委托 `TypeReference`。

1. 委托识别：

   - 解析 parent 链，确认类型最终继承 `System.MulticastDelegate`。
   - 优先使用外部 CLR 依赖中的 `System.MulticastDelegate`；未启用外部依赖优化时使用本地对应类型引用。
   - parent 无法解析时记录诊断，不仅依靠类型名称猜测。

2. 为非泛型委托创建可用的最小委托定义：

   - 保留类型名称、命名空间、可见性和 sealed 等 flags。
   - base type 指向 `System.MulticastDelegate`。
   - 创建标准 `.ctor(object, IntPtr)` 签名。
   - 从 metadata 中的 `Invoke` 方法恢复返回类型和参数。
   - 如果 metadata 提供 `BeginInvoke`、`EndInvoke`，建立对应方法骨架。
   - 委托方法使用 runtime/managed 语义，不生成普通方法体。

3. 对泛型委托：

   - 第二阶段至少创建保持名称、arity 和继承关系的类型骨架。
   - 作为成员类型引用时，按 3.4 节规则暂时替换为 `System.Object`。
   - 完整 generic parameter、约束和封闭实例留到第三阶段。

4. 如果 `Invoke` 签名包含第二阶段尚未支持的复杂类型，对对应返回值或参数应用相同的占位规则，并记录诊断。
5. 委托类型节点和其方法节点加入统一的 type/method 映射，第三阶段必须复用，不能重复生成。

### 3.6 第三遍：生成字段

对每个 type definition：

1. 按 `FieldStart` 和 `FieldCount` 创建全部字段。
2. 写入：

   - 字段名。
   - `FieldAttributes`。
   - 当前阶段可解析的字段类型。
   - literal 常量。
   - enum 的 `value__` 等特殊字段。

3. 对非 literal 字段增加 `FieldOffsetAttribute`：

   - offset 来自 `Il2CppFieldDefinition.Offset`。
   - attribute constructor 来自模板 DLL。
   - 非法或越界 offset 记录诊断，不静默写入。

4. 字段常量写入前，分别验证目标 Cecil 类型和实际常量值是否兼容。
5. 对 static、literal、init-only 等标志与 `dump.cs` 输出进行对照。

### 3.7 生成自动属性及最小访问器

1. 从 `GetMethodIndex` 和 `SetMethodIndex` 找到源 method metadata。
2. 创建属性需要的最小 getter/setter `MethodDefinition`：

   - 创建准确的访问器名称、flags、返回类型和参数。
   - 不在本阶段添加 RVA。
   - 不在本阶段承诺全部普通方法覆盖。
   - 泛型方法和复杂签名先使用诊断明确的占位引用。

3. 属性类型解析顺序：

   - 优先使用 getter 返回类型。
   - 没有 getter 时使用 setter 的 value 参数类型。
   - getter/setter 同时存在时校验两者类型一致。

4. 创建 `PropertyDefinition` 并绑定 getter/setter。
5. 对同时具有 getter 和 setter 的普通读写属性，假设其为自动属性：

   - 优先查找 metadata 中已经存在且类型兼容的 `<PropertyName>k__BackingField`，避免重复字段。
   - 如果不存在匹配字段，则创建 private synthetic backing field，并在验证清单中明确标记为 synthetic，避免被误报为 metadata 多余字段。
   - instance getter 使用 `ldarg.0`、`ldfld`、`ret`。
   - instance setter 使用 `ldarg.0`、`ldarg.1`、`stfld`、`ret`。
   - static getter/setter 分别使用 `ldsfld` 和 `stsfld`。
   - backing field 的类型必须与属性类型一致；不一致时停止为该属性生成自动属性 IL，并报告错误。

6. 以下情况不强行生成 backing field IL：

   - interface 或 abstract accessor。
   - 只有 getter 或只有 setter 的属性。
   - 带索引参数的属性。
   - accessor 为 extern/runtime 等不应含普通 IL body 的方法。

   对这些属性只保留合法的最小访问器签名和 Cecil 允许的方法体形态，并记录采用的降级路径。

7. 记录以下异常：

   - getter/setter 方法下标越界。
   - setter 没有 value 参数。
   - getter 和 setter 类型不一致。
   - 访问器声明类型与属性声明类型不一致。

8. 第三阶段生成全部方法时复用已经创建的访问器和 backing field，不得重复创建同一 metadata method 或同名 backing field。

### 3.8 第二阶段测试

增加小型合成 fixture，覆盖：

1. 普通类、接口、结构和枚举。
2. 顶层和嵌套类型。
3. 跨程序集继承和接口。
4. instance、static、literal 和 init-only 字段。
5. 字段常量和字段偏移。
6. 只读、只写和读写属性。
7. 读写自动属性复用已有 backing field，以及缺失时创建 synthetic backing field。
8. instance/static 自动属性的 getter/setter IL 正确。
9. interface、abstract 和 indexer 属性不会错误生成 backing field IL。
10. 非泛型委托定义、`Invoke` 签名及委托字段。
11. CLR 泛型集合成员保留外层集合类型，非 CLR 泛型成员替换为 `System.Object`。
12. 暂不支持的复杂类型引用能够产生可统计诊断。

样本验证重点：

1. 非泛型类型数量。
2. class/struct/enum/interface 分类。
3. 嵌套关系。
4. 字段和属性数量。
5. 字段名称、flags、常量和 offset。
6. 委托类型、`Invoke` 签名及引用这些委托的字段。
7. CLR 集合泛型和 `object` 占位诊断数量。
8. 每个输出 DLL 都能被 Cecil 和 dnSpy/ILSpy 打开。

### 3.9 第二阶段验收标准

- 所有非泛型 class、struct、enum、interface 和 delegate 均有对应 Cecil 定义。
- 非泛型类型的嵌套关系、父类和基础接口可以正常浏览。
- 字段和属性的存在性覆盖率为 100%。
- primitive 和直接 CLASS/VALUETYPE 引用准确。
- 非泛型委托字段引用真实的 delegate `TypeReference`，不得因委托未生成而退化为 `object`。
- CLR 集合泛型保留真实的集合类型；其他泛型成员类型有明确的 `object` 占位和诊断。
- 字段 offset 和 literal 常量与 `dump.cs` 一致。
- 普通读写属性可在 dnSpy/ILSpy 中显示为带 backing field 的自动属性，并关联最小 getter/setter。
- 所有暂不支持的复杂引用都有可统计的诊断，不允许无日志丢成员。

---

## 4. 第三阶段：完整方法、复杂类型、RVA 和全量验证

### 4.1 阶段目标

完成泛型及复合类型图，生成所有方法和事件，写入与 Il2CppDumper 同语义的地址信息，并对超过 100 MB 的 `dump.cs` 做全量、低内存覆盖验证。

### 4.2 补齐 `Il2CppType.bits` 和 byref

1. 读取当前尚未解析的 `Il2CppType` bits。
2. 对照 Il2CppDumper v27.2 前后布局验证：

   - `byref`
   - `pinned`
   - `num_mods`

3. 先读取样本 `Il2CppType` 的第 11 字节及其周边 bits，统计实际分布。
4. 使用样本中的 `ref`/`out` 参数及 `dump.cs` 表现确定 HSR 实际布局。
5. 将最终布局放入版本配置，避免重新硬编码。
6. 增加“原始 16 字节 → 解析字段”的单元测试和边界测试。

### 4.3 完成 Cecil `TypeReference` 图

按类型标签逐项实现：

1. primitive。
2. CLASS / VALUETYPE。
3. GENERICINST。
4. ARRAY，包括正确的 rank。
5. SZARRAY。
6. PTR。
7. VAR。
8. MVAR。
9. byref。
10. 样本中出现的其他已知 IL2CPP 包装类型。

实现约束：

1. 不再通过 `Il2CppType.Name()` 生成字符串类型。
2. CLASS/VALUETYPE 的 `Data` 直接解释为 TypeDef 下标。
3. GENERICINST 使用 startup-metadata 的 `typeDefIndex + instIndex`。
4. ARRAY/SZARRAY/PTR 中的地址按 HSR 的 VA/RVA 规则解析。
5. VAR 绑定到所属类型的 `GenericParameter`。
6. MVAR 绑定到所属方法的 `GenericParameter`。
7. 一个 generic parameter 不得跨 owner 错误复用。
8. 缓存已经解析的类型，避免在大规模成员遍历时重复读取同一图节点。
9. 处理递归类型图时加入循环检测和清晰诊断。
10. 将第二阶段中非 CLR 集合泛型成员的 `System.Object` 占位替换为真实 `GenericInstanceType`。
11. 最终严格模式下，样本中不得遗留因阶段能力不足产生的 `System.Object` 占位引用。

### 4.4 补齐泛型类型和泛型约束

1. 为类型创建全部 generic parameters。
2. 保留参数名称和 attributes。
3. 添加 generic constraints。
4. 处理嵌套泛型类型的参数归属和可见性。
5. 验证开放泛型、封闭 `GenericInstanceType` 及跨程序集泛型引用。
6. 确保 generic parameter 的缓存键包含 metadata 下标和 owner，避免同名参数冲突。

### 4.5 生成全部方法

1. 复用第二阶段已经创建的属性访问器和委托方法节点。
2. 为其余 metadata method 创建 `MethodDefinition`。
3. 写入：

   - 方法名。
   - `MethodAttributes`。
   - 返回类型。
   - 参数名称、attributes 和类型。
   - 泛型参数及约束。
   - 构造函数、静态构造函数和普通方法。
   - interface、abstract、virtual、static 等语义。

4. 当前 HSR 没有 `iflags` 时使用 `MethodImplAttributes = 0`。
5. 为需要方法体的方法生成最小合法 IL：

   - `void`：直接 `ret`。
   - value type：初始化局部变量并返回默认值。
   - reference type：返回 `null`。
   - abstract/interface 方法不创建方法体。
   - delegate 类型按 Il2CppDumper 的特殊规则处理。

6. 对 ref/out 参数使用 `ByReferenceType`，同时保留可获得的参数 attributes。
7. 如果 parameter default value 可以可靠读取，则写入；否则记录诊断，不伪造默认值。
8. 第二阶段已经生成的委托 `.ctor`、`Invoke`、`BeginInvoke` 和 `EndInvoke` 只补齐类型信息和 metadata 映射，不生成普通 IL body。

### 4.6 完成属性和事件

1. 使用完整方法节点重新核对属性 getter/setter。
2. 保留第二阶段确定的自动属性语义，使用最终解析的属性类型重新校验 backing field 和访问器 IL。
3. 移除第二阶段为复杂类型创建的临时占位引用；如果属性类型由 `object` 恢复为泛型类型，同时更新 backing field、getter 返回类型和 setter value 参数。
4. 生成事件并关联：

   - add method。
   - remove method。
   - raise/invoke method。

5. 验证属性和事件访问器属于正确的声明类型。
6. 验证一个访问器不会错误关联到多个无关成员。

### 4.7 添加 RVA、VA 和文件 Offset

对具有有效 method pointer 且非 abstract 的方法添加 `AddressAttribute`：

```text
VA     = MethodPointer
RVA    = MethodPointer - EffectiveImageBase
Offset = PEHelper.RvaToOffset(RVA)
```

处理规则：

1. ImageBase 必须来自统一版本配置。
2. method pointer 为 `0` 时不添加地址 attribute。
3. RVA 小于 0、超出 PE section 或无法映射时报告错误。
4. 当前缺少 slot 时不伪造；模板字段允许缺省时直接省略。
5. 原始 token 缺失时默认不添加 `TokenAttribute`。
6. 如果未来增加“合成 token”选项，应明确标记为合成值，不能与原始 metadata token 混淆。
7. 地址 attribute 写入后，重新读取自定义 attribute，验证三个字段的字符串和值均正确。

### 4.8 明确本轮不实现的功能

以下内容不作为第三阶段验收阻塞项：

- attribute blob 恢复。
- `CustomAttributeDataReader`。
- IDA/Ghidra 脚本。
- 官方 Registration 扫描。
- AssetStudio 的全面兼容。
- 无数据来源的原始 token、slot、property attrs 和 method iflags。

这些能力如果后续需要，应在 DummyDll 基线稳定后单独设计和验收。

---

## 5. 100 MB 以上 `dump.cs` 的验证方案

当前样本 `sample/HSR/dump/dump.cs` 约为 136 MB。验证代码不得使用 `File.ReadAllText`、完整 Roslyn 语法树或全量字符串数组。

验证建立三个相互校验的数据视图：

```text
MetadataCache 统计
        ↓
dump.cs 流式规范化清单
        ↔
DummyDll Cecil 规范化清单
```

### 5.1 流式解析 `dump.cs`

1. 使用 `FileStream + StreamReader` 单遍读取。
2. 维护轻量状态：

   - 当前 assembly。
   - 当前 namespace。
   - 当前嵌套类型栈。
   - 当前 section：Fields、Properties、Methods 或 Events。
   - 当前 TypeDefIndex。

3. 每读到一条定义，立即转换为规范化记录：

```text
T|assembly|namespace|declaring-type|kind|name
F|assembly|full-type|field-name|field-type|flags|offset|constant
P|assembly|full-type|property-name|property-type|get|set
M|assembly|full-type|method-name|generic-arity|return-type|parameters|flags|VA|RVA
E|assembly|full-type|event-name|event-type|add|remove|raise
```

4. 类型名称需要统一处理：

   - Cecil 与 `dump.cs` 的嵌套类型分隔符。
   - 泛型反引号 arity 和尖括号表示。
   - 数组 rank。
   - 指针和 byref。
   - VAR/MVAR 参数。
   - 构造函数名称和空参数名。

5. 流式解析器只支持本项目 `DumpWriter` 的确定格式，不承担通用 C# 解析职责。
6. synthetic backing field 使用独立记录标记；它不参与“metadata 字段多余项”判断，但必须参与 DLL 结构和自动属性 IL 验证。

### 5.2 从 DummyDll 提取规范化清单

1. 每次只打开一个本地生成的 DLL。
2. 递归遍历类型及其成员。
3. 读取 `FieldOffsetAttribute` 和 `AddressAttribute`。
4. 输出与 `dump.cs` 相同格式的规范化记录。
5. 处理完一个程序集立即释放 Cecil 对象和文件句柄。
6. 同时记录 Cecil 无法解析的 assembly reference、type reference 和 custom attribute。
7. 如果启用了外部依赖优化，另外建立依赖解析清单：记录外部 assembly identity、来源、被引用类型以及解析结果，不把外部库全部成员伪装成本项目生成结果。

### 5.3 有界内存排序和比较

由于 `dump.cs` 的生成顺序与 DLL 遍历顺序可能不同，不能直接逐行比较。采用外部归并：

1. 规范化记录按固定内存上限分块，例如每块 32–64 MB。
2. 每块在内存中使用 ordinal 规则排序并写入临时文件。
3. 对所有已经排序的块执行 k 路归并。
4. 对 dump manifest 和 DLL manifest 做双流 diff。
5. 比较过程中只保留当前记录和少量错误样本。
6. 最终报告包括：

   - 每类记录总数。
   - 缺失记录数。
   - 多余记录数。
   - 类型或签名不一致数。
   - RVA/offset 不一致数。
   - 每类前若干条差异。
   - 完整差异报告文件路径。

该方案的峰值内存约为“一个排序块 + 一个程序集”，不会随 `dump.cs` 总大小线性增长。

### 5.4 覆盖率门禁

最终样本验收标准：

| 检查项 | 要求 |
|---|---:|
| image/assembly 数量 | 100% 一致 |
| 类型存在性和分类 | 100% 一致 |
| 嵌套关系 | 100% 一致 |
| 字段存在性 | 100% 一致 |
| 字段类型、flags、offset、常量 | 无未解释差异 |
| 属性存在性和访问器 | 100% 一致 |
| 自动属性 backing field/IL | 全部合法；synthetic 字段单独报告 |
| 方法存在性和签名 | 100% 一致 |
| 非零方法指针的 VA/RVA | 100% 一致 |
| 事件及访问器 | 100% 一致 |
| Cecil 重新加载 | 所有 DLL 成功 |
| 未解析类型占位 | 0 |
| 未解析索引错误 | 0 |

对于 `dump.cs` 没有表达的信息，只进行 `MetadataCache → DummyDll` 的直接校验，并在报告中单独列出，不能伪装成 dump 覆盖结果。

如果启用 CLR/Unity 外部依赖优化，详细的字段、属性和方法覆盖门禁只统计需要本地生成的游戏程序集；被外部化的程序集使用独立门禁，要求 assembly identity 匹配、所有实际引用类型均可解析且不存在错误回退。报告必须同时给出两个范围，不能通过排除外部程序集人为提高本地覆盖率。

### 5.5 测试分层

1. 普通 CI：

   - 命令行和配置单元测试。
   - 小型合成 metadata fixture。
   - 类型引用、字段、方法、属性和 RVA 的定向测试。
   - 小型 DLL 写入和重新读取测试。

2. 大样本测试：

   - 标记为 `LargeSample` 或 `Integration`。
   - 显式运行，不加入默认 CI。
   - HSR 三件套继续保持 gitignore，不提交大文件。
   - 只保存紧凑的统计摘要和差异报告。

3. 确定性测试：

   - 同一输入连续生成两次。
   - 比较 DLL 文件列表。
   - 比较规范化 manifest。
   - 必要时比较文件哈希；如 Cecil 写入包含非确定字段，应先定位并规范化该字段。

4. 人工抽查：

   - 使用 dnSpy/ILSpy 打开 `mscorlib` 和游戏主程序集。
   - 抽查普通类、结构、枚举、泛型类型和嵌套类型。
   - 抽查数组、指针、ref/out 参数。
   - 抽查字段 offset 和带有效 RVA 的方法。

---

## 6. 第三阶段验收标准

- 所有输出程序集都可被 Mono.Cecil、dnSpy 或 ILSpy 正常打开。
- 泛型、数组、指针、byref、VAR/MVAR 均形成正确的 Cecil 类型图。
- 全部方法、参数、属性和事件完成关联。
- 有效方法地址包含与原版 Il2CppDumper 同语义的 VA、RVA 和文件 Offset。
- 对 136 MB 级别 `dump.cs` 的全量比较能在有界内存下完成。
- 覆盖报告不存在未解释的缺失项或复杂类型占位。
- `dump.cs` 和 `stringliterals.json` 的原有输出行为没有回归。

---

## 7. 风险和实施注意事项

### 7.1 属性依赖方法节点

Mono.Cecil 的属性 getter/setter 必须引用 `MethodDefinition`。第二阶段会把普通读写属性视为自动属性，创建或复用 backing field，并生成最小访问器 IL。第三阶段必须复用并补全这些节点，不能重新创建重复方法或 backing field。interface、abstract、单访问器和 indexer 属性不应被错误套用普通自动属性 IL。

### 7.2 HSR 类型 Data 语义与官方实现不同

CLASS/VALUETYPE、GENERICINST、ARRAY、SZARRAY、PTR、VAR/MVAR 的 `Data` 不能统一按官方 `MapVATR` 解释。所有分支必须沿用设计文档中已经确认的 HSR 语义。

### 7.3 版本配置不能只改入口

ImageBase、结构步长和解密常量当前散落在多个类型中。只在入口增加参数但不向底层传递，会形成“日志显示新值、解析仍使用旧硬编码”的隐蔽错误，因此第一阶段必须完成配置传递链和关键常量收口。

### 7.4 内存和资源释放

样本的 GameAssembly 和 metadata 本身已经占用较多内存。验证阶段应避免同时保留全部 Cecil assembly、完整 `dump.cs` 文本和全量差异集合；程序集应逐个读取和释放，清单使用分块排序。

### 7.5 适配层和生成层职责

HSR 解密、索引和 VA 解释只能存在于适配层；Cecil 生成层只消费规范化数据。这样才能在升级游戏版本或 Il2CppDumper 生成逻辑时，分别定位解析问题和输出问题。

### 7.6 不伪造缺失信息

当前 HSR 解析层没有可靠提供 token、slot、property attrs、method iflags 等信息时，应省略或使用明确约定的默认值，并写入诊断。不得生成看似来自 metadata 的伪造数据。

### 7.7 外部 CLR/Unity 依赖的兼容性

将 CLR 或 Unity 类型外部化可以显著减少生成量，但也可能引入 assembly identity、Unity 版本、类型转发和 API 缺失问题。该优化必须保持可关闭、可回退；不能让外部依赖解析成为三个核心阶段的前置阻塞条件，也不能把第三方二进制作为本仓库资源重新分发。

---

## 8. 最终完成定义

以下条件全部满足时，DummyDll 迁移可以视为完成：

1. 默认配置和显式 `OSPRODWin4.5.0` 配置产生相同的规范化输出。
2. `--dummy-dll` 不影响现有 dump 和 string literal 输出。
3. 每个 image 都有对应的有效本地程序集，或有经过 identity 和类型解析验证的外部依赖映射。
4. 类型、委托、字段、属性、方法和事件达到约定的全量覆盖。
5. 所有支持的 `Il2CppType` 均生成结构化 Cecil 类型引用，无字符串拼装和 `object` 占位。
6. 所有有效 method pointer 都有正确的 VA、RVA 和文件 Offset。
7. 大型 `dump.cs` 覆盖比较通过，且不存在未解释差异。
8. 小型测试可以在普通 CI 中运行，大样本测试有明确的本地执行命令和报告格式。
9. 使用的 Il2CppDumper commit、许可证、模板 DLL 来源和构建方式均已文档化。
10. 如果启用 CLR/Unity 外部依赖优化，依赖来源、版本、identity、回退结果和未解析类型均已写入可复现报告。
