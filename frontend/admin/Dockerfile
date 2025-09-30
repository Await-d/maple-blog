# Maple Blog Admin Frontend Dockerfile
# 多阶段构建，针对管理后台前端

FROM node:20-alpine AS base
WORKDIR /app

RUN apk add --no-cache \
    libc6-compat \
    curl \
    && rm -rf /var/cache/apk/*

COPY package.json ./
COPY package-lock.json ./

FROM base AS deps
RUN npm ci

FROM base AS builder
RUN npm ci
COPY . .

ENV NODE_ENV=production
ENV VITE_API_URL=/api
RUN npm run build
RUN find ./dist -name "*.map" -delete 2>/dev/null || true

FROM nginx:alpine AS production

RUN apk add --no-cache \
    curl \
    bash \
    && rm -rf /var/cache/apk/*

COPY --from=builder /app/dist /usr/share/nginx/html
COPY ../nginx.conf /etc/nginx/nginx.conf
COPY default.conf /etc/nginx/conf.d/default.conf

RUN addgroup -g 1001 -S nodejs && \
    adduser -S adminapp -u 1001
RUN chown -R adminapp:nodejs /usr/share/nginx/html
RUN chown -R adminapp:nodejs /var/cache/nginx
RUN chown -R adminapp:nodejs /var/log/nginx
RUN chown -R adminapp:nodejs /etc/nginx/conf.d
RUN touch /var/run/nginx.pid && chown adminapp:nodejs /var/run/nginx.pid

USER adminapp

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
  CMD curl -f http://localhost/health || exit 1

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
