// Maple Blog API文档自定义JavaScript

(function() {
    'use strict';

    // 等待Swagger UI加载完成
    function waitForSwaggerUI(callback) {
        if (window.ui && window.ui.getState) {
            callback();
        } else {
            setTimeout(() => waitForSwaggerUI(callback), 100);
        }
    }

    // 初始化自定义功能
    function initCustomFeatures() {
        // 添加自定义头部信息
        addCustomHeader();

        // 增强认证体验
        enhanceAuthentication();

        // 添加快捷操作
        addQuickActions();

        // 统计和分析
        addAnalytics();

        // 键盘快捷键
        addKeyboardShortcuts();
    }

    // 添加自定义头部
    function addCustomHeader() {
        const topbar = document.querySelector('.topbar');
        if (topbar && !document.querySelector('.custom-header')) {
            const header = document.createElement('div');
            header.className = 'custom-header';
            header.innerHTML = `
                <div style="display: flex; align-items: center; gap: 20px; padding: 10px 20px; background: rgba(255,255,255,0.1); margin-top: 10px; border-radius: 6px;">
                    <div style="color: white; font-weight: 500;">
                        <span style="font-size: 14px;">🍁 Maple Blog API</span>
                        <span style="font-size: 12px; opacity: 0.8; margin-left: 10px;">基于.NET 10构建</span>
                    </div>
                    <div style="margin-left: auto; display: flex; gap: 10px;">
                        <a href="/health" target="_blank" style="color: white; text-decoration: none; font-size: 12px; opacity: 0.8;">健康检查</a>
                        <a href="/metrics" target="_blank" style="color: white; text-decoration: none; font-size: 12px; opacity: 0.8;">监控指标</a>
                    </div>
                </div>
            `;
            topbar.appendChild(header);
        }
    }

    // 增强认证体验
    function enhanceAuthentication() {
        // 监听认证状态变化
        if (window.ui) {
            const originalUpdateLayout = window.ui.updateLayout;
            window.ui.updateLayout = function() {
                originalUpdateLayout.call(this);
                updateAuthStatus();
            };
        }

        // 添加认证状态指示器
        setTimeout(() => {
            const authSection = document.querySelector('.auth-wrapper');
            if (authSection && !document.querySelector('.auth-status')) {
                const statusDiv = document.createElement('div');
                statusDiv.className = 'auth-status';
                statusDiv.style.cssText = 'padding: 10px; text-align: center; font-size: 14px;';
                authSection.appendChild(statusDiv);
                updateAuthStatus();
            }
        }, 1000);
    }

    // 更新认证状态
    function updateAuthStatus() {
        const statusDiv = document.querySelector('.auth-status');
        if (!statusDiv) return;

        const authInputs = document.querySelectorAll('input[name="Authorization"]');
        const isAuthenticated = Array.from(authInputs).some(input => input.value.trim());

        if (isAuthenticated) {
            statusDiv.innerHTML = '<span style="color: #4caf50;">✓ 已认证</span>';
            statusDiv.style.background = '#e8f5e8';
        } else {
            statusDiv.innerHTML = '<span style="color: #ff9800;">⚠ 未认证 - 部分API需要身份验证</span>';
            statusDiv.style.background = '#fff3e0';
        }
    }

    // 添加快捷操作
    function addQuickActions() {
        // 添加全局操作栏
        if (!document.querySelector('.quick-actions')) {
            const actionsBar = document.createElement('div');
            actionsBar.className = 'quick-actions';
            actionsBar.style.cssText = `
                position: fixed;
                bottom: 20px;
                right: 20px;
                z-index: 1000;
                display: flex;
                flex-direction: column;
                gap: 10px;
            `;

            // 返回顶部按钮
            const backToTopBtn = document.createElement('button');
            backToTopBtn.innerHTML = '↑';
            backToTopBtn.title = '返回顶部';
            backToTopBtn.style.cssText = `
                width: 50px;
                height: 50px;
                border-radius: 50%;
                background: #d32f2f;
                color: white;
                border: none;
                font-size: 20px;
                font-weight: bold;
                cursor: pointer;
                box-shadow: 0 2px 8px rgba(0,0,0,0.2);
                transition: all 0.2s ease;
            `;
            backToTopBtn.addEventListener('click', () => {
                window.scrollTo({ top: 0, behavior: 'smooth' });
            });

            // 折叠/展开所有
            const toggleAllBtn = document.createElement('button');
            toggleAllBtn.innerHTML = '⚌';
            toggleAllBtn.title = '折叠/展开所有';
            toggleAllBtn.style.cssText = backToTopBtn.style.cssText;
            toggleAllBtn.addEventListener('click', toggleAllOperations);

            actionsBar.appendChild(backToTopBtn);
            actionsBar.appendChild(toggleAllBtn);
            document.body.appendChild(actionsBar);

            // 滚动时显示/隐藏
            let isScrolling = false;
            window.addEventListener('scroll', () => {
                if (!isScrolling) {
                    actionsBar.style.opacity = window.scrollY > 200 ? '1' : '0.5';
                    isScrolling = true;
                    setTimeout(() => { isScrolling = false; }, 100);
                }
            });
        }
    }

    // 折叠/展开所有操作
    function toggleAllOperations() {
        const operations = document.querySelectorAll('.opblock');
        const expandedOps = document.querySelectorAll('.opblock.is-open');

        if (expandedOps.length > operations.length / 2) {
            // 如果超过一半是展开的，就全部折叠
            operations.forEach(op => {
                if (op.classList.contains('is-open')) {
                    const summary = op.querySelector('.opblock-summary');
                    if (summary) summary.click();
                }
            });
        } else {
            // 否则全部展开
            operations.forEach(op => {
                if (!op.classList.contains('is-open')) {
                    const summary = op.querySelector('.opblock-summary');
                    if (summary) summary.click();
                }
            });
        }
    }

    // 添加统计和分析
    function addAnalytics() {
        // 简单的使用统计
        const stats = {
            pageViews: parseInt(localStorage.getItem('swagger-page-views') || '0') + 1,
            apiCalls: parseInt(localStorage.getItem('swagger-api-calls') || '0'),
            lastVisit: new Date().toISOString()
        };

        localStorage.setItem('swagger-page-views', stats.pageViews.toString());
        localStorage.setItem('swagger-last-visit', stats.lastVisit);

        // 监听API调用
        const originalFetch = window.fetch;
        window.fetch = function(...args) {
            const url = args[0];
            if (typeof url === 'string' && url.includes('/api/')) {
                stats.apiCalls++;
                localStorage.setItem('swagger-api-calls', stats.apiCalls.toString());
            }
            return originalFetch.apply(this, args);
        };

        // 在控制台显示统计信息
        console.log('📊 Swagger UI 统计信息:', stats);
    }

    // 添加键盘快捷键
    function addKeyboardShortcuts() {
        document.addEventListener('keydown', (e) => {
            // Ctrl+F 或 Cmd+F 聚焦到搜索框
            if ((e.ctrlKey || e.metaKey) && e.key === 'f') {
                e.preventDefault();
                const searchInput = document.querySelector('.operation-filter-input');
                if (searchInput) {
                    searchInput.focus();
                    searchInput.select();
                }
            }

            // Esc 清除搜索
            if (e.key === 'Escape') {
                const searchInput = document.querySelector('.operation-filter-input');
                if (searchInput && searchInput === document.activeElement) {
                    searchInput.value = '';
                    searchInput.dispatchEvent(new Event('change'));
                    searchInput.blur();
                }
            }

            // 数字键快速跳转到标签
            if (e.key >= '1' && e.key <= '9' && !e.ctrlKey && !e.metaKey && !e.altKey) {
                const tagIndex = parseInt(e.key) - 1;
                const tags = document.querySelectorAll('.opblock-tag-section');
                if (tags[tagIndex]) {
                    tags[tagIndex].scrollIntoView({ behavior: 'smooth', block: 'start' });
                }
            }
        });

        // 添加快捷键说明
        setTimeout(() => {
            if (!document.querySelector('.keyboard-shortcuts')) {
                const shortcutsDiv = document.createElement('div');
                shortcutsDiv.className = 'keyboard-shortcuts';
                shortcutsDiv.style.cssText = `
                    position: fixed;
                    top: 10px;
                    right: 10px;
                    background: rgba(0,0,0,0.8);
                    color: white;
                    padding: 8px 12px;
                    border-radius: 4px;
                    font-size: 12px;
                    opacity: 0;
                    transition: opacity 0.3s ease;
                    pointer-events: none;
                    z-index: 1001;
                `;
                shortcutsDiv.innerHTML = `
                    <div>快捷键: Ctrl+F 搜索 | Esc 清除 | 1-9 跳转标签</div>
                `;

                // 鼠标悬停在页面右上角时显示
                let showTimeout;
                document.addEventListener('mousemove', (e) => {
                    if (e.clientX > window.innerWidth - 100 && e.clientY < 100) {
                        clearTimeout(showTimeout);
                        shortcutsDiv.style.opacity = '1';
                    } else {
                        showTimeout = setTimeout(() => {
                            shortcutsDiv.style.opacity = '0';
                        }, 2000);
                    }
                });

                document.body.appendChild(shortcutsDiv);
            }
        }, 2000);
    }

    // 错误处理和重试机制
    window.addEventListener('error', (e) => {
        console.warn('Swagger UI自定义脚本错误:', e.error);
    });

    // 页面加载完成后初始化
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => {
            waitForSwaggerUI(initCustomFeatures);
        });
    } else {
        waitForSwaggerUI(initCustomFeatures);
    }

    // 导出一些实用函数到全局
    window.MapleBlogSwagger = {
        toggleAllOperations,
        updateAuthStatus,
        version: '1.0.0'
    };

})();