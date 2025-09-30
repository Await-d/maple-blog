/**
 * AboutPage - Maple Blog关于页面
 * 提供平台介绍、功能特色、技术栈信息和团队介绍
 */

import React from 'react';
import { Helmet } from '@/components/common/DocumentHead';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Badge } from '../components/ui/badge';
import { Button } from '../components/ui/Button';
import { cn } from '../utils/cn';
import {
  Heart,
  Users,
  Zap,
  Shield,
  Smartphone,
  Globe,
  Code,
  Database,
  Cloud,
  Palette,
  Search,
  MessageCircle,
  Edit,
  BarChart3,
  Github,
  ExternalLink
} from 'lucide-react';

// 页面属性接口
interface AboutPageProps {
  className?: string;
}

// 功能特色数据
const features = [
  {
    icon: <Edit className="h-8 w-8 text-blue-500" />,
    title: '智能写作',
    description: '现代化的Markdown编辑器，支持实时预览、语法高亮和智能补全，让写作变得更加高效。'
  },
  {
    icon: <Users className="h-8 w-8 text-green-500" />,
    title: '社区互动',
    description: '完整的评论系统、点赞功能和用户关注，构建活跃的技术社区，促进知识分享。'
  },
  {
    icon: <Search className="h-8 w-8 text-purple-500" />,
    title: '智能搜索',
    description: '强大的全文搜索功能，支持标签筛选、分类浏览和个性化推荐，快速找到感兴趣的内容。'
  },
  {
    icon: <Shield className="h-8 w-8 text-red-500" />,
    title: '安全可靠',
    description: 'JWT认证、角色权限控制、数据加密传输，确保用户数据安全和平台稳定运行。'
  },
  {
    icon: <Smartphone className="h-8 w-8 text-orange-500" />,
    title: '响应式设计',
    description: '完美适配桌面端、平板和移动设备，无论何时何地都能享受优质的阅读和写作体验。'
  },
  {
    icon: <BarChart3 className="h-8 w-8 text-indigo-500" />,
    title: '数据分析',
    description: '详细的文章统计、用户行为分析和内容洞察，帮助作者了解读者喜好和优化内容。'
  }
];

// 技术栈数据
const techStack = {
  frontend: [
    { name: 'React 19', description: '最新的React框架，提供并发特性和服务器组件' },
    { name: 'TypeScript', description: '类型安全的JavaScript，提升开发效率和代码质量' },
    { name: 'Vite', description: '极速的前端构建工具，支持热重载和优化构建' },
    { name: 'Tailwind CSS', description: '实用优先的CSS框架，快速构建现代界面' },
    { name: 'Zustand', description: '轻量级状态管理库，简单高效的状态管理' },
    { name: 'TanStack Query', description: '强大的数据获取和缓存解决方案' }
  ],
  backend: [
    { name: '.NET 10', description: '微软最新的跨平台开发框架，高性能和现代化' },
    { name: 'ASP.NET Core', description: 'Web API框架，支持RESTful服务和实时通信' },
    { name: 'Entity Framework Core', description: 'ORM框架，简化数据库操作和管理' },
    { name: 'SQLite/PostgreSQL', description: '灵活的数据库支持，从开发到生产无缝切换' },
    { name: 'JWT Authentication', description: '安全的令牌认证机制，支持多设备登录' },
    { name: 'FluentValidation', description: '强大的数据验证框架，确保数据完整性' }
  ],
  infrastructure: [
    { name: 'Docker', description: '容器化部署，确保环境一致性和可扩展性' },
    { name: 'Redis', description: '高性能缓存和会话存储，提升应用响应速度' },
    { name: 'Serilog', description: '结构化日志记录，便于监控和问题诊断' },
    { name: 'Swagger/OpenAPI', description: '自动生成API文档，便于开发和集成' },
    { name: 'GitHub Actions', description: 'CI/CD自动化流程，确保代码质量和部署效率' },
    { name: 'HTTPS/SSL', description: '全站HTTPS加密，保障数据传输安全' }
  ]
};

export const AboutPage: React.FC<AboutPageProps> = ({ className }) => {
  return (
    <>
      {/* SEO 元数据 */}
      <Helmet>
        <title>关于我们 - Maple Blog</title>
        <meta name="description" content="了解Maple Blog - 现代化的AI驱动博客平台。探索我们的技术特色、功能亮点和技术栈，加入我们的技术社区。" />
        <meta name="keywords" content="Maple Blog, 技术博客, React 19, .NET 10, 开源项目, 技术社区" />
        <meta name="author" content="Maple Blog Team" />

        {/* Open Graph */}
        <meta property="og:type" content="website" />
        <meta property="og:title" content="关于我们 - Maple Blog" />
        <meta property="og:description" content="现代化的AI驱动博客平台，基于React 19和.NET 10构建" />
        <meta property="og:url" content="https://maple-blog.com/about" />
        <meta property="og:image" content="https://maple-blog.com/images/about-og.jpg" />

        {/* Twitter Card */}
        <meta name="twitter:card" content="summary_large_image" />
        <meta name="twitter:title" content="关于我们 - Maple Blog" />
        <meta name="twitter:description" content="现代化的AI驱动博客平台，基于React 19和.NET 10构建" />

        {/* 结构化数据 */}
        <script type="application/ld+json">
          {JSON.stringify({
            '@context': 'https://schema.org',
            '@type': 'Organization',
            'name': 'Maple Blog',
            'description': '现代化的AI驱动博客平台',
            'url': 'https://maple-blog.com',
            'logo': 'https://maple-blog.com/logo.png',
            'foundingDate': '2024',
            'sameAs': [
              'https://github.com/maple-blog'
            ]
          })}
        </script>
      </Helmet>

      <div className={cn('min-h-screen bg-gray-50 dark:bg-gray-950', className)}>
        {/* 英雄区域 */}
        <section className="bg-gradient-to-r from-orange-500 to-red-600 text-white py-20">
          <div className="container-responsive">
            <div className="max-w-4xl mx-auto text-center">
              <h1 className="text-4xl md:text-6xl font-bold mb-6">
                关于 Maple Blog
              </h1>
              <p className="text-xl md:text-2xl opacity-90 mb-8 leading-relaxed">
                现代化的AI驱动博客平台，为技术分享和知识交流而生
              </p>
              <div className="flex flex-col sm:flex-row items-center justify-center space-y-4 sm:space-y-0 sm:space-x-6">
                <div className="flex items-center space-x-2">
                  <Heart className="h-5 w-5 text-red-200" />
                  <span>用心构建</span>
                </div>
                <div className="flex items-center space-x-2">
                  <Zap className="h-5 w-5 text-yellow-200" />
                  <span>性能卓越</span>
                </div>
                <div className="flex items-center space-x-2">
                  <Globe className="h-5 w-5 text-blue-200" />
                  <span>开源开放</span>
                </div>
              </div>
            </div>
          </div>
        </section>

        {/* 平台介绍 */}
        <section className="py-16 bg-white dark:bg-gray-900">
          <div className="container-responsive">
            <div className="max-w-4xl mx-auto">
              <div className="text-center mb-12">
                <h2 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-6">
                  我们的使命
                </h2>
                <p className="text-lg text-gray-600 dark:text-gray-400 leading-relaxed">
                  Maple Blog 致力于为技术爱好者、开发者和知识分享者提供一个现代化、高效、安全的博客平台。
                  我们相信知识的力量，致力于构建一个促进技术交流、激发创新思维的社区环境。
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
                <Card className="text-center">
                  <CardContent className="pt-6">
                    <div className="w-16 h-16 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Code className="h-8 w-8 text-blue-600 dark:text-blue-400" />
                    </div>
                    <h3 className="text-xl font-semibold mb-3">技术驱动</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      采用最新的技术栈，确保平台的性能、安全性和可扩展性
                    </p>
                  </CardContent>
                </Card>

                <Card className="text-center">
                  <CardContent className="pt-6">
                    <div className="w-16 h-16 bg-green-100 dark:bg-green-900 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Users className="h-8 w-8 text-green-600 dark:text-green-400" />
                    </div>
                    <h3 className="text-xl font-semibold mb-3">社区优先</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      以用户为中心，构建友好的社区环境，促进知识分享和交流
                    </p>
                  </CardContent>
                </Card>

                <Card className="text-center">
                  <CardContent className="pt-6">
                    <div className="w-16 h-16 bg-purple-100 dark:bg-purple-900 rounded-full flex items-center justify-center mx-auto mb-4">
                      <Heart className="h-8 w-8 text-purple-600 dark:text-purple-400" />
                    </div>
                    <h3 className="text-xl font-semibold mb-3">开源精神</h3>
                    <p className="text-gray-600 dark:text-gray-400">
                      拥抱开源文化，与社区共同成长，推动技术进步
                    </p>
                  </CardContent>
                </Card>
              </div>
            </div>
          </div>
        </section>

        {/* 功能特色 */}
        <section className="py-16 bg-gray-50 dark:bg-gray-950">
          <div className="container-responsive">
            <div className="max-w-6xl mx-auto">
              <div className="text-center mb-12">
                <h2 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-6">
                  功能特色
                </h2>
                <p className="text-lg text-gray-600 dark:text-gray-400">
                  丰富的功能特性，为您提供完整的博客体验
                </p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
                {features.map((feature, index) => (
                  <Card key={index} className="hover:shadow-lg transition-shadow duration-300">
                    <CardContent className="pt-6">
                      <div className="flex items-center space-x-4 mb-4">
                        <div className="flex-shrink-0">
                          {feature.icon}
                        </div>
                        <h3 className="text-xl font-semibold text-gray-900 dark:text-white">
                          {feature.title}
                        </h3>
                      </div>
                      <p className="text-gray-600 dark:text-gray-400 leading-relaxed">
                        {feature.description}
                      </p>
                    </CardContent>
                  </Card>
                ))}
              </div>
            </div>
          </div>
        </section>

        {/* 技术栈 */}
        <section className="py-16 bg-white dark:bg-gray-900">
          <div className="container-responsive">
            <div className="max-w-6xl mx-auto">
              <div className="text-center mb-12">
                <h2 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-6">
                  技术栈
                </h2>
                <p className="text-lg text-gray-600 dark:text-gray-400">
                  基于现代化技术栈构建，确保性能、安全和可维护性
                </p>
              </div>

              <div className="space-y-8">
                {/* 前端技术 */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-3">
                      <Palette className="h-6 w-6 text-blue-500" />
                      <span>前端技术</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {techStack.frontend.map((tech, index) => (
                        <div key={index} className="p-4 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                          <div className="flex items-center justify-between mb-2">
                            <h4 className="font-semibold text-gray-900 dark:text-white">{tech.name}</h4>
                            <Badge variant="secondary">前端</Badge>
                          </div>
                          <p className="text-sm text-gray-600 dark:text-gray-400">{tech.description}</p>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                {/* 后端技术 */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-3">
                      <Database className="h-6 w-6 text-green-500" />
                      <span>后端技术</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {techStack.backend.map((tech, index) => (
                        <div key={index} className="p-4 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                          <div className="flex items-center justify-between mb-2">
                            <h4 className="font-semibold text-gray-900 dark:text-white">{tech.name}</h4>
                            <Badge variant="secondary">后端</Badge>
                          </div>
                          <p className="text-sm text-gray-600 dark:text-gray-400">{tech.description}</p>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>

                {/* 基础设施 */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center space-x-3">
                      <Cloud className="h-6 w-6 text-purple-500" />
                      <span>基础设施</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                      {techStack.infrastructure.map((tech, index) => (
                        <div key={index} className="p-4 border rounded-lg hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors">
                          <div className="flex items-center justify-between mb-2">
                            <h4 className="font-semibold text-gray-900 dark:text-white">{tech.name}</h4>
                            <Badge variant="secondary">基础设施</Badge>
                          </div>
                          <p className="text-sm text-gray-600 dark:text-gray-400">{tech.description}</p>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          </div>
        </section>

        {/* 联系我们 */}
        <section className="py-16 bg-gray-50 dark:bg-gray-950">
          <div className="container-responsive">
            <div className="max-w-4xl mx-auto text-center">
              <h2 className="text-3xl md:text-4xl font-bold text-gray-900 dark:text-white mb-6">
                加入我们
              </h2>
              <p className="text-lg text-gray-600 dark:text-gray-400 mb-8">
                欢迎加入Maple Blog社区，一起分享知识，共同成长
              </p>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mb-8">
                <Card>
                  <CardContent className="pt-6 text-center">
                    <Github className="h-12 w-12 mx-auto mb-4 text-gray-700 dark:text-gray-300" />
                    <h3 className="text-xl font-semibold mb-3">开源贡献</h3>
                    <p className="text-gray-600 dark:text-gray-400 mb-4">
                      参与开源项目，提交代码贡献，一起完善平台功能
                    </p>
                    <Button variant="outline" onClick={() => window.open('https://github.com/maple-blog', '_blank')}>
                      <Github className="h-4 w-4 mr-2" />
                      查看源码
                      <ExternalLink className="h-4 w-4 ml-2" />
                    </Button>
                  </CardContent>
                </Card>

                <Card>
                  <CardContent className="pt-6 text-center">
                    <MessageCircle className="h-12 w-12 mx-auto mb-4 text-gray-700 dark:text-gray-300" />
                    <h3 className="text-xl font-semibold mb-3">社区交流</h3>
                    <p className="text-gray-600 dark:text-gray-400 mb-4">
                      加入我们的社区，与其他开发者交流技术经验
                    </p>
                    <Button variant="outline">
                      <MessageCircle className="h-4 w-4 mr-2" />
                      加入讨论
                    </Button>
                  </CardContent>
                </Card>
              </div>

              <div className="flex flex-col sm:flex-row items-center justify-center space-y-4 sm:space-y-0 sm:space-x-4">
                <Button size="lg" onClick={() => window.location.href = '/register'}>
                  立即注册
                </Button>
                <Button variant="outline" size="lg" onClick={() => window.location.href = '/blog'}>
                  浏览文章
                </Button>
              </div>
            </div>
          </div>
        </section>

        {/* 页脚信息 */}
        <section className="py-8 bg-gray-800 dark:bg-gray-900 text-white">
          <div className="container-responsive">
            <div className="max-w-4xl mx-auto text-center">
              <p className="text-gray-300 mb-4">
                © 2024 Maple Blog. 基于 React 19 和 .NET 10 构建的现代化博客平台
              </p>
              <div className="flex items-center justify-center space-x-6 text-sm text-gray-400">
                <span>版本 1.0.0</span>
                <span>•</span>
                <span>开源项目</span>
                <span>•</span>
                <span>MIT许可证</span>
              </div>
            </div>
          </div>
        </section>
      </div>
    </>
  );
};

export default AboutPage;