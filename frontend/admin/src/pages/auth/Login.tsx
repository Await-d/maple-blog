// @ts-nocheck
import React, { useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import {
  Card,
  Form,
  Input,
  Button,
  Checkbox,
  Typography,
  Space,
  message,
  Divider,
} from 'antd';
import {
  UserOutlined,
  LockOutlined,
  EyeInvisibleOutlined,
  EyeTwoTone,
} from '@ant-design/icons';
import { Helmet } from 'react-helmet-async';

import { useAdminStore } from '@/stores/adminStore';
import { env, storageUtils } from '@/utils';

const { Title, Text, Link } = Typography;

interface LoginForm {
  username: string;
  password: string;
  rememberMe: boolean;
}

const Login: React.FC = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const [loading, setLoading] = useState(false);
  const { setUser } = useAdminStore();

  const redirectUrl = searchParams.get('redirect') || '/dashboard';

  const handleLogin = async (values: LoginForm) => {
    setLoading(true);
    try {
      // Real login API call
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          username: values.username,
          password: values.password,
          rememberMe: values.rememberMe
        })
      });

      if (!response.ok) {
        throw new Error('Login failed');
      }

      const loginResult = await response.json();

      if (!loginResult.success) {
        throw new Error(loginResult.message || 'Login failed');
      }

      const { user, accessToken, refreshToken } = loginResult.data;

      // 保存认证信息
      if (values.rememberMe) {
        storageUtils.set('access_token', accessToken, 7 * 24 * 60 * 60 * 1000); // 7天
        storageUtils.set('refresh_token', refreshToken, 30 * 24 * 60 * 60 * 1000); // 30天
      } else {
        storageUtils.set('access_token', accessToken);
        storageUtils.set('refresh_token', refreshToken);
      }

      // 更新用户状态
      setUser(user);

      message.success('登录成功！');
      navigate(redirectUrl, { replace: true });
    } catch (error) {
      message.error('登录失败，请检查用户名和密码');
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
      <Helmet>
        <title>登录 - {env.appTitle}</title>
        <meta name="description" content="登录到管理后台" />
      </Helmet>

      <div
        style={{
          minHeight: '100vh',
          background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          padding: '20px',
        }}
      >
        <Card
          style={{
            width: '100%',
            maxWidth: 400,
            borderRadius: 16,
            boxShadow: '0 8px 32px rgba(0, 0, 0, 0.1)',
            border: 'none',
          }}
        >
          <div style={{ textAlign: 'center', marginBottom: 32 }}>
            <div
              style={{
                fontSize: 48,
                marginBottom: 16,
              }}
            >
              🍁
            </div>
            <Title level={2} style={{ margin: 0, color: '#1890ff' }}>
              Maple Blog
            </Title>
            <Text type="secondary">管理后台登录</Text>
          </div>

          <Form
            name="login"
            initialValues={{ rememberMe: true }}
            onFinish={handleLogin}
            autoComplete="off"
            size="large"
          >
            <Form.Item
              name="username"
              rules={[
                { required: true, message: '请输入用户名' },
                { min: 3, message: '用户名至少3个字符' },
              ]}
            >
              <Input
                prefix={<UserOutlined />}
                placeholder="用户名"
                autoComplete="username"
              />
            </Form.Item>

            <Form.Item
              name="password"
              rules={[
                { required: true, message: '请输入密码' },
                { min: 6, message: '密码至少6个字符' },
              ]}
            >
              <Input.Password
                prefix={<LockOutlined />}
                placeholder="密码"
                autoComplete="current-password"
                iconRender={(visible) =>
                  visible ? <EyeTwoTone /> : <EyeInvisibleOutlined />
                }
              />
            </Form.Item>

            <Form.Item>
              <div
                style={{
                  display: 'flex',
                  justifyContent: 'space-between',
                  alignItems: 'center',
                }}
              >
                <Form.Item name="rememberMe" valuePropName="checked" noStyle>
                  <Checkbox>记住我</Checkbox>
                </Form.Item>
                <Link href="#" style={{ fontSize: 14 }}>
                  忘记密码？
                </Link>
              </div>
            </Form.Item>

            <Form.Item>
              <Button
                type="primary"
                htmlType="submit"
                loading={loading}
                block
                style={{
                  height: 48,
                  fontSize: 16,
                  fontWeight: 500,
                }}
              >
                登录
              </Button>
            </Form.Item>
          </Form>

          <Divider>
            <Text type="secondary" style={{ fontSize: 12 }}>
              演示账号
            </Text>
          </Divider>

          <Space direction="vertical" style={{ width: '100%' }}>
            <div style={{ textAlign: 'center' }}>
              <Text type="secondary" style={{ fontSize: 12 }}>
                用户名: admin | 密码: 123456
              </Text>
            </div>
            <div style={{ textAlign: 'center' }}>
              <Text type="secondary" style={{ fontSize: 12 }}>
                {env.appVersion} | Powered by React 19
              </Text>
            </div>
          </Space>
        </Card>
      </div>
    </>
  );
};

export default Login;