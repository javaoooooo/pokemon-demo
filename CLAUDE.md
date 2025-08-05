# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 项目概述

**项目名称：** Petomagica World (PMW) - 2D Demo 版本  

**项目类型：** 基于位置的2D宠物收集对战游戏  

**当前阶段：** 早期开发阶段，专注MVP功能实现  

PMW 是一款结合现实世界地理位置的宠物收集游戏。玩家在真实地图上探索，发现并捕捉宠物，进行训练和对战。第一版demo专注于2D实现，AR功能将在后续版本中添加。

## 核心技术栈

### 客户端
- **Unity 2022.3 LTS** - 2D游戏引擎
- **Mapbox SDK for Unity** - 地图服务和位置功能
- **Unity Netcode for GameObjects** - 多人网络同步
- **Unity Addressables** - 资源管理系统

### 后端服务
- **Node.js + Express** - RESTful API服务器
- **Socket.IO** - 实时通信（位置同步、对战）
- **PostgreSQL + PostGIS** - 主数据库（地理位置查询优化）
- **Redis** - 缓存层（会话管理、实时数据）
- **JWT** - 身份验证

### 开发工具
- **Git** - 版本控制
- **Stable Diffusion** - AI生成游戏资源
- **Postman** - API测试

## 开发阶段规划

### 第一阶段：基础框架（2周）
**目标：** 搭建可运行的地图系统
- [ ] Unity 2D项目初始化
- [ ] Mapbox SDK集成和配置
- [ ] 玩家GPS定位和地图显示
- [ ] 基础UI框架搭建
- [ ] Node.js后端API框架
- [ ] PostgreSQL数据库设置

### 第二阶段：宠物系统（2周）
**目标：** 实现宠物发现和捕捉功能
- [ ] 宠物生成点系统（基于地理位置）
- [ ] 捕捉界面和交互逻辑
- [ ] 宠物数据模型和存储
- [ ] 背包/收藏系统
- [ ] 宠物属性展示界面

### 第三阶段：对战系统（2周）
**目标：** 基础回合制对战功能
- [ ] 回合制战斗系统
- [ ] 技能系统（4个基础技能）
- [ ] AI对手逻辑
- [ ] 经验值和等级系统
- [ ] 宠物进化机制

### 第四阶段：联网功能（1周）
**目标：** 多人基础功能
- [ ] 实时位置同步
- [ ] 玩家发现系统
- [ ] PvP对战匹配
- [ ] 数据持久化优化

## 游戏资源清单

### AI生成资源（Stable Diffusion）
**宠物图像：**
- 5种基础宠物类型：火、水、草、光、暗
- 每种3个进化阶段 = 15张宠物立绘
- 图像规格：512x512px，透明背景
- 提示词模板：`"cute [type] [creature] pokemon style, 2D game art, clean background, kawaii"`

**技能图标：**
- 攻击、防御、治疗、特殊技能各1个
- 图像规格：128x128px，图标风格

### 手工/免费资源
**UI素材：**
- 血条、经验条（CSS渐变）
- 按钮样式（Unity内置UI）
- 地图标记图标（简单几何图形）

**音效：**
- Freesound.org免费音效库
- Unity内置音效包

## 项目架构

### 客户端目录结构
```
Assets/
├── Scripts/
│   ├── Managers/
│   │   ├── GameManager.cs         # 游戏状态管理
│   │   ├── MapManager.cs          # 地图和定位管理
│   │   ├── PetManager.cs          # 宠物系统管理
│   │   ├── BattleManager.cs       # 对战系统管理
│   │   └── NetworkManager.cs      # 网络通信管理
│   ├── UI/
│   │   ├── MapUI.cs               # 地图界面
│   │   ├── PetBagUI.cs            # 宠物背包界面
│   │   ├── BattleUI.cs            # 对战界面
│   │   └── CaptureUI.cs           # 捕捉界面
│   ├── Data/
│   │   ├── PetData.cs             # 宠物数据结构
│   │   ├── PlayerData.cs          # 玩家数据结构
│   │   └── BattleData.cs          # 对战数据结构
│   └── Utils/
│       ├── LocationService.cs     # GPS位置服务
│       └── APIClient.cs           # HTTP API客户端
├── Prefabs/                       # 预制体资源
├── Sprites/                       # 2D图像资源
└── Audio/                         # 音频资源
```

### 后端目录结构
```
server/
├── routes/
│   ├── auth.js                    # 身份验证API
│   ├── pets.js                    # 宠物相关API
│   ├── players.js                 # 玩家数据API
│   └── battles.js                 # 对战功能API
├── models/
│   ├── User.js                    # 用户数据模型
│   ├── Pet.js                     # 宠物数据模型
│   ├── Battle.js                  # 对战记录模型
│   └── Location.js                # 位置数据模型
├── services/
│   ├── locationService.js         # 地理位置处理
│   ├── battleService.js           # 对战逻辑处理
│   └── petSpawnService.js         # 宠物生成逻辑
├── middleware/
│   ├── auth.js                    # JWT验证中间件
│   └── validation.js              # 数据验证中间件
└── config/
    ├── database.js                # 数据库配置
    └── redis.js                   # Redis配置
```

## 核心数据模型

### 宠物数据结构
```json
{
  "id": "pet_fire_001",
  "name": "火焰幼龙",
  "type": "fire",
  "level": 1,
  "exp": 0,
  "evolutionStage": 1,
  "stats": {
    "hp": 100,
    "maxHp": 100,
    "attack": 20,
    "defense": 15,
    "speed": 12
  },
  "skills": ["火球术", "防御"],
  "captureLocation": {
    "lat": 31.2304,
    "lng": 121.4737,
    "timestamp": "2024-08-04T08:00:00Z"
  }
}
```

### 玩家数据结构
```json
{
  "id": "player_001",
  "username": "小二",
  "level": 1,
  "exp": 0,
  "location": {
    "lat": 31.2304,
    "lng": 121.4737,
    "lastUpdate": "2024-08-04T08:00:00Z"
  },
  "pets": ["pet_fire_001"],
  "activePet": "pet_fire_001",
  "stats": {
    "totalCaptured": 1,
    "battlesWon": 0,
    "battlesLost": 0
  }
}
```

## 开发指导原则

### 代码规范
- **C# (Unity)：** 使用PascalCase命名，遵循Unity编码规范
- **JavaScript (Node.js)：** 使用camelCase命名，ESLint标准配置
- **数据库：** 使用snake_case命名约定

### 性能考虑
- 宠物生成使用地理围栏，避免频繁GPS查询
- 实时对战数据使用Redis缓存，减少数据库压力
- 客户端使用对象池管理UI元素，优化内存使用

### 安全措施
- 位置数据验证，防止GPS欺骗
- 对战结果服务器验证，防止客户端作弊
- API接口限流，防止恶意请求

### 测试策略
- 单人模式优先开发，便于调试
- 使用模拟位置数据进行室内测试
- 分阶段集成测试，确保每个模块独立可用

## 常用开发命令

### Unity开发
- **构建项目：** File → Build Settings → Build
- **包管理：** Window → Package Manager
- **调试：** Window → General → Console

### 后端开发
```bash
# 启动开发服务器
npm run dev

# 运行测试
npm test

# 数据库迁移
npm run migrate

# Redis缓存清理
npm run redis:flush
```

### 数据库操作
```sql
-- 创建宠物生成点
INSERT INTO spawn_points (type, lat, lng, active) VALUES ('fire', 31.2304, 121.4737, true);

-- 查询附近宠物
SELECT * FROM spawn_points WHERE ST_DWithin(ST_Point(lng, lat), ST_Point(121.4737, 31.2304), 0.001);
```

## 部署配置

### 开发环境
- Unity: 本地开发
- Node.js: localhost:3000
- PostgreSQL: localhost:5432
- Redis: localhost:6379

### 生产环境准备
- Unity Cloud Build（可选）
- 云服务器部署（阿里云/腾讯云）
- CDN加速静态资源
- SSL证书配置

---

**备注：** 这是一个快速开发的MVP版本规划，专注于核心功能验证。后续版本将添加更多高级功能如AR、公会系统、经济系统等。