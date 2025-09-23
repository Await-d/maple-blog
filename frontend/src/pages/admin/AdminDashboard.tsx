// @ts-nocheck
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
              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      用户管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      管理用户账户和权限
                    </p>
                  </div>
                  <Users className="w-8 h-8 text-blue-600 dark:text-blue-400" />
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      内容管理
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      管理文章和评论
                    </p>
                  </div>
                  <FileText className="w-8 h-8 text-green-600 dark:text-green-400" />
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      数据统计
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      查看访问和使用统计
                    </p>
                  </div>
                  <BarChart3 className="w-8 h-8 text-purple-600 dark:text-purple-400" />
                </div>
              </div>

              <div className="bg-white dark:bg-gray-800 rounded-lg shadow-md p-6">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">
                      系统设置
                    </h3>
                    <p className="text-gray-600 dark:text-gray-400 text-sm">
                      配置系统参数
                    </p>
                  </div>
                  <Settings className="w-8 h-8 text-orange-600 dark:text-orange-400" />
                </div>
              </div>
            </div>

            {/* 占位内容 */}
            <div className="text-center py-16">
              <div className="inline-flex items-center justify-center w-24 h-24 bg-blue-100 dark:bg-blue-900 rounded-full mb-6">
                <Shield className="w-12 h-12 text-blue-600 dark:text-blue-400" />
              </div>
              <h2 className="text-2xl font-semibold text-gray-900 dark:text-white mb-4">
                管理功能开发中
              </h2>
              <p className="text-gray-600 dark:text-gray-400 max-w-md mx-auto">
                我们正在构建功能完善的管理界面，包括用户管理、内容审核、数据分析等功能。
              </p>
            </div>
          </div>
        </div>
      </div>
    </>
  );
};

export default AdminDashboard;