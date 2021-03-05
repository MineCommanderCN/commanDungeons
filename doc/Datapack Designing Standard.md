# 内容包设计规范

## 总体文件规范

### 编码

所有文件统一使用UTF-8编码。

### JSON

使用JSON.Net可读取的标准JSON文件格式。

### JavaScript

支持ECMAScript 3/5标准语法、ES5严格模式、部分ECMAScript 6语法。

详细特性请查看我们使用的Javascript库——Jurassic的文档：<https://github.com/paulbartrum/jurassic#ecmascript-6-status>

### 图例和术语

#### JSON文件图例

```text
(N) 数字
(B) 布尔值
(S) 字符串
{}  集合
[(*)] 某种类型的列表，例如[(N)]为数字列表
```

#### 通用脚本

一般的JavaScript脚本，无返回值。

#### 战利品脚本（LS）

用于敌人死亡时和新房间生成时生成掉落物。脚本本身需返回一个仅含`format.itemStack`类型的数组（Array）或对象。

#### 实体构建脚本（ES）

用于新房间生成时产生敌人实体。脚本本身需返回一个仅含`format.entity`类型的数组（Array）或对象。

## 目录结构总览

```text
数据包根目录
|-  registry.json    注册配置文件
|-  global_event.json   全局事件脚本注册
|-  data    数据相关文件
    |-  (命名空间)
       |-  effect    状态效果
       |-  enemy     敌人
       |-  item      物品
       |-  level     关卡
|-  language    语言文件
|-  script      脚本
    |-  (命名空间)
|-  commands    自定义命令
    |-  (命名空间)
```

## 注册文件 `registry.json`

`registry.json`是用于注册内容包的包名、格式版本、源数据等基本信息的文件。

### 格式

```text
(S) pack_name: 内容包的注册名。原则上不能与其他包重复。一般格式为"pack.(作者、组织或域名).(英文名或简写)"。
(N) file_format: 内容包版本格式，用于检查内容包是否兼容当前游戏版本。
{}  meta_data: 元数据辞典。一般需要包含description与creator。同样支持自定义类型的数据，可以在语言文件中添加自定义类型的翻译名。
```

## 状态效果 `effect`

```text
(B)   debuff: 决定此效果是否为负面效果（显示时加红）
[{}]  attribute_modifiers: 属性修饰符。
{}    scripts: 脚本事件注册。使用通用脚本。
```
