# MS / Micro Service（v0.0.1）

事业微服务，跟某一个游戏 IP 一一对应，复用同一套框架。v0.0.1 只求跑通一条链路：

```
客户端 → MP.UserService 登录，拿 JWT
       → 带着 JWT 请求 MS.Gateway
       → Gateway 校验 JWT，转发到 MS.UserService / MS.Game
```

本版本砍掉了 Leaderboard / BugReport / Dashboard / RabbitMQ / Redis / Chat / Mail，
只保留 Gateway + UserService + Game(空壳) 三个服务 + 一个 Postgres，先把链路走通。

## 服务清单

| 服务 | 端口(宿主机) | 职责 |
|------|------|------|
| gateway | 6000 | 校验 MP 签发的 JWT，反向代理到 userservice / game |
| userservice | 6001 | 游戏内用户档案（uid 复用 MP 的 uid），get-or-create |
| game | 6002 | 空壳，只有 /health 和 /api/game/ping，游戏逻辑之后再填 |
| postgres | 9433 | MS 自己的库 `ms_gameuser`，跟 MP 的库物理隔离 |

> 注意端口跟 MP 的 compose（5000/5001/9432/6379）故意错开，本机可以两套 compose 同时跑，
> 互相之间**不需要**共享 docker 网络——JWT 是对称密钥签名，MS 这边本地校验，不用回调 MP。

## 前置条件

MS 和 MP 的 `Jwt:Issuer` / `Jwt:Audience` / `Jwt:SecretKey` 必须完全一致（本仓库已经按 MP 现在
`appsettings.json` 里的默认值预置好了）。如果你后面改了 MP 的密钥，记得同步改这边 `docker-compose.yml`
里 gateway/userservice 的环境变量。

## 启动

```bash
# 先确保 MP 那边已经跑起来（另一个 compose 项目）
cd MP && docker compose up -d

# 再启动 MS
cd MS && docker compose up -d --build
```

## 联调流程（完整走一遍）

```bash
# 1. 在 MP 注册一个账号
curl -X POST http://localhost:5001/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"test@example.com","password":"123456"}'

# 2. 在 MP 登录，拿 token（把响应里的 token 存到变量里）
TOKEN=$(curl -s -X POST http://localhost:5001/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"123456"}' | jq -r .token)

# 3. 拿着 MP 发的 token，走 MS 的网关，第一次访问会自动建游戏内档案
curl http://localhost:6000/api/users/me \
  -H "Authorization: Bearer $TOKEN"

# 4. 验证 Gateway → Game 这条转发链路
curl http://localhost:6000/api/game/ping \
  -H "Authorization: Bearer $TOKEN"
```

第 3 步如果返回类似：

```json
{ "uid": "...", "nickname": "Player_ab12cd", "level": 1, "createdAt": "..." }
```

说明 `MP 登录 → MS 网关鉴权 → MS 落库` 这条链路完整打通，v0.0.1 目标达成。

## 目录结构

```
MS/
├─ MS.slnx
├─ docker-compose.yml
└─ src/
   ├─ MS.Shared/          # JwtOptions，Gateway和UserService共用
   ├─ MS.Gateway/         # JWT校验 + 反向代理
   ├─ MS.UserService/     # 游戏内用户档案，Dapper.AOT + Postgres
   └─ MS.Game/            # 空壳，游戏逻辑TODO
```

## 后续版本 TODO（不影响 v0.0.1 验收）

- MS.Game 接入 LiteNetLib + MemoryPack，换 UDP 协议
- 加回 Leaderboard（Redis ZSET）
- Game 服支持多副本 + Redis 做跨实例状态
- EFCore 迁移文件化（目前用 `EnsureCreated`，表结构稳定后切回 `dotnet ef migrations`）
- BugReport / Dashboard 独立镜像
