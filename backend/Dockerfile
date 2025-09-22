# Maple Blog Backend Dockerfile
# .NET 10 多阶段构建优化

# 阶段1: 构建环境
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# 复制项目文件
COPY ["src/MapleBlog.API/MapleBlog.API.csproj", "src/MapleBlog.API/"]
COPY ["src/MapleBlog.Application/MapleBlog.Application.csproj", "src/MapleBlog.Application/"]
COPY ["src/MapleBlog.Domain/MapleBlog.Domain.csproj", "src/MapleBlog.Domain/"]
COPY ["src/MapleBlog.Infrastructure/MapleBlog.Infrastructure.csproj", "src/MapleBlog.Infrastructure/"]

# 还原依赖
RUN dotnet restore "src/MapleBlog.API/MapleBlog.API.csproj"

# 复制源代码
COPY . .
WORKDIR "/src/src/MapleBlog.API"

# 构建应用
RUN dotnet build "MapleBlog.API.csproj" -c Release -o /app/build

# 阶段2: 发布
FROM build AS publish
RUN dotnet publish "MapleBlog.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 阶段3: 生产运行时
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS production
WORKDIR /app

# 安装运行时依赖
RUN apt-get update && apt-get install -y \
    curl \
    wget \
    dumb-init \
    && rm -rf /var/lib/apt/lists/*

# 复制发布文件
COPY --from=publish /app/publish .

# 创建非root用户
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN chown -R appuser:appuser /app
USER appuser

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
  CMD curl -f http://localhost:80/health || exit 1

# 暴露端口
EXPOSE 80
EXPOSE 443

# 环境变量
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV DOTNET_USE_POLLING_FILE_WATCHER=true

# 启动应用
ENTRYPOINT ["dumb-init", "--"]
CMD ["dotnet", "MapleBlog.API.dll"]

# 构建参数和标签
ARG BUILD_DATE
ARG VCS_REF
ARG VERSION
LABEL maintainer="Maple Blog Team"
LABEL org.label-schema.build-date=$BUILD_DATE
LABEL org.label-schema.name="maple-blog-backend"
LABEL org.label-schema.description="Maple Blog .NET 10 Backend API"
LABEL org.label-schema.vcs-ref=$VCS_REF
LABEL org.label-schema.vcs-url="https://github.com/maple-blog/maple-blog"
LABEL org.label-schema.vendor="Maple Blog"
LABEL org.label-schema.version=$VERSION
LABEL org.label-schema.schema-version="1.0"