# Maple Blog 管理端与用户端部署架构

## 架构概述

Maple Blog 采用前后端分离架构，后端分为两个独立的服务：

1. **MapleBlog.API** - 用户端 API 服务
   - 为普通用户提供博客浏览、评论、搜索等功能
   - 端口：5000（开发）/ 5100（生产）
   - 路径前缀：`/api`

2. **MapleBlog.Admin** - 管理端 API 服务  
   - 为管理员提供内容管理、用户管理、系统配置等功能
   - 端口：5001（开发）/ 5101（生产）
   - 路径前缀：`/admin/api`

## 部署模式

### 1. Docker Compose 部署（推荐）

系统提供了三个 Docker Compose 配置文件：

#### docker-compose.yml（开发环境）
```yaml
services:
  # 用户端 API
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
      target: development
    ports:
      - "5000:5000"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./data:/app/data
  
  # 管理端 API  
  admin-api:
    build:
      context: ./backend
      dockerfile: Dockerfile.admin
      target: development
    ports:
      - "5001:5001"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
    volumes:
      - ./data:/app/data

  # 前端（React）
  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    ports:
      - "3000:80"
    environment:
      - VITE_API_URL=http://localhost:5000/api
      - VITE_ADMIN_API_URL=http://localhost:5001/admin/api
```

#### docker-compose.prod.yml（生产环境）
```yaml
services:
  # Nginx 反向代理
  nginx:
    image: nginx:alpine
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./docker/nginx/nginx.conf:/etc/nginx/nginx.conf
      - ./docker/nginx/ssl:/etc/nginx/ssl
      - ./frontend/dist:/usr/share/nginx/html
    depends_on:
      - api
      - admin-api

  # 用户端 API
  api:
    build:
      context: ./backend
      dockerfile: Dockerfile
      target: production
    expose:
      - "5100"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=...
      - ConnectionStrings__RedisConnection=...
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
  
  # 管理端 API
  admin-api:
    build:
      context: ./backend
      dockerfile: Dockerfile.admin
      target: production
    expose:
      - "5101"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ConnectionStrings__DefaultConnection=...
      - ConnectionStrings__RedisConnection=...
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs

  # PostgreSQL 数据库
  postgres:
    image: postgres:16-alpine
    environment:
      - POSTGRES_DB=mapleblog
      - POSTGRES_USER=postgres
      - POSTGRES_PASSWORD=${DB_PASSWORD}
    volumes:
      - postgres_data:/var/lib/postgresql/data

  # Redis 缓存
  redis:
    image: redis:7-alpine
    volumes:
      - redis_data:/data
```

#### docker-compose.admin.yml（仅管理端）
```yaml
services:
  # 管理端独立部署
  admin-api:
    build:
      context: ./backend
      dockerfile: Dockerfile.admin
      target: production
    ports:
      - "5101:5101"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - AdminOnly=true
    volumes:
      - ./data:/app/data
      - ./logs:/app/logs
```

### 2. Kubernetes 部署

系统支持 Kubernetes 部署，每个服务作为独立的 Deployment：

```yaml
# api-deployment.yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mapleblog-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: mapleblog-api
  template:
    metadata:
      labels:
        app: mapleblog-api
    spec:
      containers:
      - name: api
        image: mapleblog/api:latest
        ports:
        - containerPort: 5100
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
---
# admin-api-deployment.yaml  
apiVersion: apps/v1
kind: Deployment
metadata:
  name: mapleblog-admin-api
spec:
  replicas: 2
  selector:
    matchLabels:
      app: mapleblog-admin-api
  template:
    metadata:
      labels:
        app: mapleblog-admin-api
    spec:
      containers:
      - name: admin-api
        image: mapleblog/admin-api:latest
        ports:
        - containerPort: 5101
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
```

## Dockerfile 说明

### backend/Dockerfile（用户端 API）
```dockerfile
# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MapleBlog.API/MapleBlog.API.csproj", "MapleBlog.API/"]
COPY ["src/MapleBlog.Application/", "MapleBlog.Application/"]
COPY ["src/MapleBlog.Infrastructure/", "MapleBlog.Infrastructure/"]
COPY ["src/MapleBlog.Domain/", "MapleBlog.Domain/"]
RUN dotnet restore "MapleBlog.API/MapleBlog.API.csproj"
COPY . .
WORKDIR "/src/MapleBlog.API"
RUN dotnet build -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# 生产运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5100
ENTRYPOINT ["dotnet", "MapleBlog.API.dll"]
```

### backend/Dockerfile.admin（管理端 API）
```dockerfile
# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/MapleBlog.Admin/MapleBlog.Admin.csproj", "MapleBlog.Admin/"]
COPY ["src/MapleBlog.Application/", "MapleBlog.Application/"]
COPY ["src/MapleBlog.Infrastructure/", "MapleBlog.Infrastructure/"]
COPY ["src/MapleBlog.Domain/", "MapleBlog.Domain/"]
RUN dotnet restore "MapleBlog.Admin/MapleBlog.Admin.csproj"
COPY . .
WORKDIR "/src/MapleBlog.Admin"
RUN dotnet build -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# 生产运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS production
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5101
ENTRYPOINT ["dotnet", "MapleBlog.Admin.dll"]
```

## Nginx 反向代理配置

生产环境使用 Nginx 作为反向代理，统一入口：

```nginx
# docker/nginx/nginx.conf
upstream api_backend {
    server api:5100;
}

upstream admin_backend {
    server admin-api:5101;
}

server {
    listen 80;
    server_name mapleblog.com;

    # 前端静态文件
    location / {
        root /usr/share/nginx/html;
        try_files $uri $uri/ /index.html;
    }

    # 用户端 API
    location /api/ {
        proxy_pass http://api_backend/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    # 管理端 API
    location /admin/api/ {
        proxy_pass http://admin_backend/;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
        
        # 管理端额外的安全头
        add_header X-Frame-Options "SAMEORIGIN";
        add_header X-Content-Type-Options "nosniff";
    }

    # WebSocket 支持（SignalR）
    location /hubs/ {
        proxy_pass http://api_backend/hubs/;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }

    # 文件上传
    location /uploads/ {
        alias /app/data/uploads/;
        expires 30d;
        add_header Cache-Control "public, immutable";
    }
}
```

## 部署流程

### 1. 开发环境部署
```bash
# 克隆项目
git clone https://github.com/yourusername/maple-blog.git
cd maple-blog

# 创建环境配置
cp .env.template .env
# 编辑 .env 文件配置数据库、Redis 等

# 启动所有服务
docker-compose up -d

# 查看服务状态
docker-compose ps

# 查看日志
docker-compose logs -f api
docker-compose logs -f admin-api
```

### 2. 生产环境部署
```bash
# 构建生产镜像
docker-compose -f docker-compose.prod.yml build

# 启动生产环境
docker-compose -f docker-compose.prod.yml up -d

# 应用数据库迁移
docker-compose -f docker-compose.prod.yml exec api dotnet ef database update

# 设置 SSL 证书（如果需要）
cp /path/to/ssl/cert.pem docker/nginx/ssl/
cp /path/to/ssl/key.pem docker/nginx/ssl/
docker-compose -f docker-compose.prod.yml restart nginx
```

### 3. 管理端独立部署
```bash
# 仅部署管理端
docker-compose -f docker-compose.admin.yml up -d

# 或使用 Docker 直接运行
docker build -t mapleblog-admin:latest -f backend/Dockerfile.admin backend/
docker run -d \
  -p 5101:5101 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/data:/app/data \
  --name mapleblog-admin \
  mapleblog-admin:latest
```

## 环境变量配置

### 用户端 API（MapleBlog.API）
```env
# 数据库连接
ConnectionStrings__DefaultConnection=Host=postgres;Database=mapleblog;Username=postgres;Password=yourpassword

# Redis 缓存
ConnectionStrings__RedisConnection=redis:6379

# JWT 配置
Jwt__Secret=your-secret-key-at-least-32-characters
Jwt__Issuer=https://mapleblog.com
Jwt__Audience=https://mapleblog.com

# 文件存储
Storage__Provider=Local
Storage__LocalPath=/app/data/uploads

# 邮件服务
Email__SmtpHost=smtp.gmail.com
Email__SmtpPort=587
Email__SmtpUser=your-email@gmail.com
Email__SmtpPassword=your-password
```

### 管理端 API（MapleBlog.Admin）
```env
# 继承用户端的所有配置，额外添加：

# 管理端特定配置
Admin__RequireTwoFactor=true
Admin__SessionTimeout=30
Admin__MaxLoginAttempts=5
Admin__IPWhitelist=192.168.1.0/24,10.0.0.0/8

# 审计日志
Audit__Enabled=true
Audit__RetentionDays=90
```

## 监控与日志

### 1. 健康检查端点
- 用户端：`http://localhost:5100/health`
- 管理端：`http://localhost:5101/admin/health`

### 2. Prometheus 指标
- 用户端：`http://localhost:5100/metrics`
- 管理端：`http://localhost:5101/admin/metrics`

### 3. 日志收集
系统使用 Serilog 进行日志记录，支持多种输出：
- 控制台输出
- 文件输出（`/app/logs/`）
- Elasticsearch（可选）

## 安全考虑

1. **服务隔离**：管理端和用户端 API 完全隔离，独立部署
2. **网络隔离**：生产环境中，管理端可部署在内网，仅通过 VPN 访问
3. **认证分离**：管理端使用独立的认证系统，支持双因素认证
4. **访问控制**：管理端支持 IP 白名单、访问频率限制
5. **审计日志**：所有管理操作都有审计记录
6. **数据加密**：敏感数据传输使用 HTTPS，存储使用加密

## 扩展性

1. **水平扩展**：两个 API 服务都支持水平扩展，可部署多个实例
2. **负载均衡**：通过 Nginx 或 Kubernetes Service 实现负载均衡
3. **缓存层**：使用 Redis 减少数据库压力
4. **CDN 支持**：静态资源可通过 CDN 分发
5. **微服务架构**：未来可进一步拆分为更多微服务

## 故障恢复

1. **数据备份**：定期备份 PostgreSQL 数据库和上传文件
2. **服务重启**：使用 Docker restart policies 或 Kubernetes liveness probes
3. **日志监控**：通过 ELK Stack 或类似工具监控异常
4. **降级策略**：缓存失效时自动降级到数据库直接访问