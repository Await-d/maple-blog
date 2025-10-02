# Maple Blog Frontend Dockerfile
# 多阶段构建优化镜像体积

# 阶段1: 基础环境设置
FROM node:20-alpine AS base
WORKDIR /app

# 安装系统依赖和pnpm
RUN apk add --no-cache \
    libc6-compat \
    curl \
    && rm -rf /var/cache/apk/* \
    && npm install -g pnpm

# 复制包管理文件
COPY package.json ./
COPY pnpm-lock.yaml ./

# 阶段2: 依赖安装
FROM base AS deps
RUN pnpm install --frozen-lockfile

# 阶段3: 开发环境
FROM base AS development
RUN pnpm install --frozen-lockfile
COPY . .
EXPOSE 3000
ENV NODE_ENV=development
CMD ["pnpm", "run", "dev"]

# 阶段4: 构建环境
FROM base AS builder
RUN pnpm install --frozen-lockfile
COPY . .

# 构建应用
ENV NODE_ENV=production
ENV VITE_API_URL=/api
ENV GENERATE_SOURCEMAP=false
RUN pnpm run build

# 优化构建结果（dist目录）
RUN find ./dist -name "*.map" -delete 2>/dev/null || true

# 阶段5: 生产环境
FROM nginx:alpine AS production

# 安装运行时依赖
RUN apk add --no-cache \
    curl \
    bash \
    && rm -rf /var/cache/apk/*

# 复制构建结果
COPY --from=builder /app/dist /usr/share/nginx/html

# 复制Nginx配置
COPY nginx.conf /etc/nginx/nginx.conf
COPY default.conf /etc/nginx/conf.d/default.conf

# 创建非root用户
RUN addgroup -g 1001 -S nodejs && \
    adduser -S nextjs -u 1001
RUN chown -R nextjs:nodejs /usr/share/nginx/html
RUN chown -R nextjs:nodejs /var/cache/nginx
RUN chown -R nextjs:nodejs /var/log/nginx
RUN chown -R nextjs:nodejs /etc/nginx/conf.d
RUN touch /var/run/nginx.pid
RUN chown -R nextjs:nodejs /var/run/nginx.pid

USER nextjs

# 健康检查
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

# 构建参数和标签
ARG BUILD_DATE
ARG VCS_REF
ARG VERSION
LABEL maintainer="Maple Blog Team"
LABEL org.label-schema.build-date=$BUILD_DATE
LABEL org.label-schema.name="maple-blog-frontend"
LABEL org.label-schema.description="Maple Blog React 19 Frontend"
LABEL org.label-schema.vcs-ref=$VCS_REF
LABEL org.label-schema.vcs-url="https://github.com/maple-blog/maple-blog"
LABEL org.label-schema.vendor="Maple Blog"
LABEL org.label-schema.version=$VERSION
LABEL org.label-schema.schema-version="1.0"
