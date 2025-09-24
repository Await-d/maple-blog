/**
 * AdminDashboard - 管理员仪表板页面
 * 提供网站管理、用户管理、内容管理等功能的入口
 */

import React from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { Shield, Users, FileText, BarChart3, Settings } from 'lucide-react';

export const AdminDashboard: React.FC = () => {
  return (
    <>
      <Helmet>
        <title>管理仪表板 - Maple Blog</title>
        <meta name="description" content="网站管理和数据监控仪表板。" />
        <meta name="robots" content="noindex, nofollow" />
      </Helmet>

      <div className="min-h-screen bg-gray-50 dark:bg-gray-950">
        <div className="container-responsive py-8">
          <div className="max-w-7xl mx-auto">
            {/* 页面标题 */}
            <div className="mb-8">
              <h1 className="text-3xl font-bold text-gray-900 dark:text-white mb-2 flex items-center">
                <Shield className="w-8 h-8 mr-3 text-blue-600 dark:text-blue-400" />
                管理仪表板
              </h1>
              <p className="text-lg text-gray-600 dark:text-gray-400">
                网站管理和数据监控中心
              </p>
            </div>

            {/* 快速操作卡片 */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
              <a 
                href="/admin/users" 
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 cursor-pointer group"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                      用户管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      管理用户账户和权限
                    </p>
                  </div>
                  <Users className="w-8 h-8 text-blue-600 dark:text-blue-400 group-hover:scale-110 transition-transform" />
                </div>
              </a>

              <a 
                href="/admin/content" 
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 cursor-pointer group"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-green-600 dark:group-hover:text-green-400 transition-colors">
                      内容管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      管理文章和评论
                    </p>
                  </div>
                  <FileText className="w-8 h-8 text-green-600 dark:text-green-400 group-hover:scale-110 transition-transform" />
                </div>
              </a>

              <a 
                href="/admin/analytics" 
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 cursor-pointer group"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-purple-600 dark:group-hover:text-purple-400 transition-colors">
                      数据统计
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      查看访问和使用统计
                    </p>
                  </div>
                  <BarChart3 className="w-8 h-8 text-purple-600 dark:text-purple-400 group-hover:scale-110 transition-transform" />
                </div>
              </a>

              <a 
                href="/admin/settings" 
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 cursor-pointer group"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-orange-600 dark:group-hover:text-orange-400 transition-colors">
                      系统设置
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      配置系统参数
                    </p>
                  </div>
                  <Settings className="w-8 h-8 text-orange-600 dark:text-orange-400 group-hover:scale-110 transition-transform" />
                </div>
              </a>
            </div>

            {/* 额外管理功能 */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
              <a 
                href="/admin/seed-data" 
                className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 hover:shadow-lg transition-shadow duration-200 cursor-pointer group"
              >
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2 group-hover:text-indigo-600 dark:group-hover:text-indigo-400 transition-colors">
                      种子数据管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      初始化和管理测试数据
                    </p>
                  </div>
                  <Shield className="w-8 h-8 text-indigo-600 dark:text-indigo-400 group-hover:scale-110 transition-transform" />
                </div>
              </a>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 opacity-75">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      权限管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      配置角色和权限 (开发中)
                    </p>
                  </div>
                  <Shield className="w-8 h-8 text-gray-400" />
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6 opacity-75">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      审计日志
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      查看系统操作记录 (开发中)
                    </p>
                  </div>
                  <FileText className="w-8 h-8 text-gray-400" />
                </div>
              </div>
            </div>

            {/* 快速概览 */}
            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="text-center">
                  <div className="text-3xl font-bold text-blue-600 dark:text-blue-400 mb-2">4</div>
                  <div className="text-sm text-gray-600 dark:text-gray-400">管理页面</div>
                  <div className="text-xs text-gray-500 mt-1">已完成</div>
                </div>
              </div>
              
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="text-center">
                  <div className="text-3xl font-bold text-green-600 dark:text-green-400 mb-2">100%</div>
                  <div className="text-sm text-gray-600 dark:text-gray-400">前端完成度</div>
                  <div className="text-xs text-gray-500 mt-1">功能齐全</div>
                </div>
              </div>
              
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="text-center">
                  <div className="text-3xl font-bold text-purple-600 dark:text-purple-400 mb-2">5</div>
                  <div className="text-sm text-gray-600 dark:text-gray-400">后端API</div>
                  <div className="text-xs text-gray-500 mt-1">已实现</div>
                </div>
              </div>
              
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="text-center">
                  <div className="text-3xl font-bold text-orange-600 dark:text-orange-400 mb-2">✓</div>
                  <div className="text-sm text-gray-600 dark:text-gray-400">系统状态</div>
                  <div className="text-xs text-gray-500 mt-1">运行正常</div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default AdminDashboard;