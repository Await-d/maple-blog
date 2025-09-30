import React, { useState, useEffect } from 'react';
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter as _CardFooter,
  CardHeader,
  CardTitle,
} from '@/components/ui/card';
import { Button } from '@/components/ui/Button';
import { Badge } from '@/components/ui/badge';
import { Alert, AlertDescription } from '@/components/ui/alert';
import {
  Tabs,
  TabsContent,
  TabsList,
  TabsTrigger,
} from '@/components/ui/tabs';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Progress as _Progress } from '@/components/ui/progress';
import { Separator as _Separator } from '@/components/ui/separator';
import { errorReporter } from '@/services/errorReporting';
import { toast } from '@/services/toastNotification';
import { showValidation, ValidationResult as ValidationDisplayResult } from '@/components/common/ValidationDisplay';
import {
  CheckCircle,
  AlertTriangle,
  XCircle,
  Database,
  RefreshCw,
  Trash2,
  Settings,
  Clock,
  Users,
  FileText,
  Tags,
  Folder,
  Shield,
  Activity,
  Download as _Download,
  Upload,
} from 'lucide-react';

interface SeedStatus {
  environment: string;
  checkTime: string;
  isHealthy: boolean;
  canConnectToDatabase: boolean;
  hasTestData: boolean;
  existingUsers: number;
  existingPosts: number;
  existingCategories: number;
  existingTags: number;
  existingRoles: number;
  issues: string[];
  recommendations: string[];
}

interface SeedProvider {
  environment: string;
  priority: number;
  type: string;
  canProvideFor: boolean;
}

interface ValidationResult {
  isValid: boolean;
  errors: string[];
  warnings: string[];
  context: Record<string, unknown>;
}

interface SeedResult {
  environment: string;
  isSuccess: boolean;
  errorMessage?: string;
  requiresConfirmation: boolean;
  duration: string;
  totalCreated: number;
  totalSkipped: number;
  rolesCreated: number;
  usersCreated: number;
  categoriesCreated: number;
  tagsCreated: number;
  postsCreated: number;
  validationErrors: string[];
  validationWarnings: string[];
}

interface CleanupResult {
  isSuccess: boolean;
  errorMessage?: string;
  isDryRun: boolean;
  testUsersFound: number;
  testPostsFound: number;
  testUsersRemoved: number;
  testPostsRemoved: number;
  duration: string;
}

const SeedDataManagement: React.FC = () => {
  const [status, setStatus] = useState<SeedStatus | null>(null);
  const [providers, setProviders] = useState<SeedProvider[]>([]);
  const [loading, setLoading] = useState(false);
  const [seedResult, setSeedResult] = useState<SeedResult | null>(null);
  const [cleanupResult, setCleanupResult] = useState<CleanupResult | null>(null);
  const [showSeedDialog, setShowSeedDialog] = useState(false);
  const [showCleanupDialog, setShowCleanupDialog] = useState(false);
  const [environment, setEnvironment] = useState('');

  const fetchStatus = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/admin/seeddata/status');
      if (response.ok) {
        const data = await response.json();
        setStatus(data);
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to load seed data status'),
        {
          component: 'SeedDataManagement',
          action: 'loadStatus'
        }
      );
      toast.error('加载种子数据状态失败，请刷新页面重试');
    } finally {
      setLoading(false);
    }
  };

  const fetchProviders = async () => {
    try {
      const response = await fetch('/api/admin/seeddata/providers');
      if (response.ok) {
        const data = await response.json();
        setProviders(data);
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to load seed data providers'),
        {
          component: 'SeedDataManagement',
          action: 'fetchProviders'
        }
      );
      toast.error('加载种子数据提供商失败，请重试');
    }
  };

  const fetchEnvironment = async () => {
    try {
      const response = await fetch('/api/admin/seeddata/environment');
      if (response.ok) {
        const data = await response.json();
        setEnvironment(data.name);
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to load environment info'),
        {
          component: 'SeedDataManagement',
          action: 'fetchEnvironment'
        }
      );
      toast.error('加载环境信息失败，请重试');
    }
  };

  const validateEnvironment = async () => {
    setLoading(true);
    try {
      const response = await fetch('/api/admin/seeddata/validate', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ environment }),
      });

      if (response.ok) {
        const validationResult: ValidationResult = await response.json();
        // Convert to ValidationDisplayResult format for display
        const displayResult: ValidationDisplayResult = {
          valid: validationResult.isValid,
          errors: validationResult.errors.map(error => ({
            field: 'environment',
            message: error
          })),
          warnings: validationResult.warnings.map(warning => ({
            field: 'environment',
            message: warning
          })),
          summary: {
            totalErrors: validationResult.errors.length,
            totalWarnings: validationResult.warnings.length,
            criticalErrors: 0
          }
        };

        // Display validation result to user
        showValidation(displayResult, {
          title: '环境验证结果',
          showSummary: true,
          showMetadata: true,
          onRetry: () => validateEnvironment()
        });

        if (validationResult.isValid) {
          toast.success('环境验证通过，可以安全执行种子数据操作');
        } else {
          toast.warning(`发现 ${validationResult.errors.length} 个环境问题`);
        }
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to validate environment'),
        {
          component: 'SeedDataManagement',
          action: 'validateEnvironment'
        }
      );
      toast.error('环境验证失败，请重试');
    } finally {
      setLoading(false);
    }
  };

  const seedData = async (forceSeeding = false, confirmProduction = false) => {
    setLoading(true);
    try {
      const response = await fetch('/api/admin/seeddata/seed', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          environment,
          forceSeeding,
          confirmProduction,
        }),
      });

      if (response.ok) {
        const result: SeedResult = await response.json();
        setSeedResult(result);
        fetchStatus(); // Refresh status
      } else {
        const error = await response.json();
        setSeedResult({
          environment,
          isSuccess: false,
          errorMessage: error.error || 'Seeding failed',
          requiresConfirmation: false,
          duration: '0',
          totalCreated: 0,
          totalSkipped: 0,
          rolesCreated: 0,
          usersCreated: 0,
          categoriesCreated: 0,
          tagsCreated: 0,
          postsCreated: 0,
          validationErrors: [],
          validationWarnings: [],
        });
        toast.error('种子数据生成失败，请检查服务器状态');
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to seed data'),
        {
          component: 'SeedDataManagement',
          action: 'seedData',
          environment,
          forceSeeding,
          confirmProduction
        }
      );
      toast.error('种子数据操作失败，请重试');
    } finally {
      setLoading(false);
      setShowSeedDialog(false);
    }
  };

  const cleanTestData = async (dryRun = true) => {
    setLoading(true);
    try {
      const response = await fetch('/api/admin/seeddata/clean-test-data', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ dryRun }),
      });

      if (response.ok) {
        const result: CleanupResult = await response.json();
        setCleanupResult(result);
        fetchStatus(); // Refresh status
        if (result.isSuccess) {
          toast.success(dryRun ? '测试数据清理预览完成' : '测试数据已成功清理');
        } else {
          toast.error('测试数据清理失败: ' + result.errorMessage);
        }
      }
    } catch (error) {
      await errorReporter.captureError(
        error instanceof Error ? error : new Error('Failed to clean test data'),
        {
          component: 'SeedDataManagement',
          action: 'cleanTestData',
          dryRun
        }
      );
      toast.error('测试数据清理操作失败，请重试');
    } finally {
      setLoading(false);
      setShowCleanupDialog(false);
    }
  };

  useEffect(() => {
    fetchStatus();
    fetchProviders();
    fetchEnvironment();
  }, []);

  const getStatusIcon = (isHealthy: boolean) => {
    return isHealthy ? (
      <CheckCircle className="h-5 w-5 text-green-500" />
    ) : (
      <XCircle className="h-5 w-5 text-red-500" />
    );
  };

  const getEnvironmentBadge = (env: string) => {
    const colors = {
      Development: 'bg-blue-100 text-blue-800',
      Staging: 'bg-yellow-100 text-yellow-800',
      Production: 'bg-red-100 text-red-800',
    };
    return colors[env as keyof typeof colors] || 'bg-gray-100 text-gray-800';
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Seed Data Management</h1>
          <p className="text-gray-600">
            Manage application seed data, initialization, and cleanup
          </p>
        </div>
        <div className="flex space-x-2">
          <Button onClick={fetchStatus} variant="outline" disabled={loading}>
            <RefreshCw className={`mr-2 h-4 w-4 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </Button>
        </div>
      </div>

      <Tabs defaultValue="status" className="space-y-4">
        <TabsList>
          <TabsTrigger value="status">Status</TabsTrigger>
          <TabsTrigger value="providers">Providers</TabsTrigger>
          <TabsTrigger value="operations">Operations</TabsTrigger>
          <TabsTrigger value="history">History</TabsTrigger>
        </TabsList>

        <TabsContent value="status">
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Environment</CardTitle>
                <Settings className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{environment}</div>
                <Badge className={getEnvironmentBadge(environment)}>
                  {environment}
                </Badge>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Database Health</CardTitle>
                <Database className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="flex items-center space-x-2">
                  {status && getStatusIcon(status.isHealthy)}
                  <div className="text-2xl font-bold">
                    {status?.isHealthy ? 'Healthy' : 'Issues'}
                  </div>
                </div>
                {status?.canConnectToDatabase ? (
                  <p className="text-xs text-muted-foreground">Connected</p>
                ) : (
                  <p className="text-xs text-red-500">Cannot connect</p>
                )}
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Test Data</CardTitle>
                <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">
                  {status?.hasTestData ? 'Present' : 'Clean'}
                </div>
                <Badge variant={status?.hasTestData ? 'destructive' : 'default'}>
                  {status?.hasTestData ? 'Needs Cleanup' : 'No Test Data'}
                </Badge>
              </CardContent>
            </Card>
          </div>

          {status && (
            <Card>
              <CardHeader>
                <CardTitle>Database Statistics</CardTitle>
                <CardDescription>Current database content overview</CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-5">
                  <div className="flex items-center space-x-2">
                    <Users className="h-4 w-4 text-blue-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Users</p>
                      <p className="text-2xl font-bold">{status.existingUsers}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <FileText className="h-4 w-4 text-green-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Posts</p>
                      <p className="text-2xl font-bold">{status.existingPosts}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Folder className="h-4 w-4 text-yellow-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Categories</p>
                      <p className="text-2xl font-bold">{status.existingCategories}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Tags className="h-4 w-4 text-purple-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Tags</p>
                      <p className="text-2xl font-bold">{status.existingTags}</p>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <Shield className="h-4 w-4 text-red-500" />
                    <div>
                      <p className="text-sm text-muted-foreground">Roles</p>
                      <p className="text-2xl font-bold">{status.existingRoles}</p>
                    </div>
                  </div>
                </div>
              </CardContent>
            </Card>
          )}

          {status?.issues && status.issues.length > 0 && (
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                <div className="space-y-1">
                  <p className="font-semibold">Issues detected:</p>
                  <ul className="list-disc list-inside space-y-1">
                    {status.issues.map((issue, index) => (
                      <li key={index} className="text-sm">{issue}</li>
                    ))}
                  </ul>
                </div>
              </AlertDescription>
            </Alert>
          )}
        </TabsContent>

        <TabsContent value="providers">
          <Card>
            <CardHeader>
              <CardTitle>Seed Data Providers</CardTitle>
              <CardDescription>
                Available providers for different environments
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {providers.map((provider, index) => (
                  <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                    <div>
                      <div className="font-semibold">{provider.type}</div>
                      <div className="text-sm text-muted-foreground">
                        Environment: {provider.environment} | Priority: {provider.priority}
                      </div>
                    </div>
                    <Badge variant={provider.canProvideFor ? 'default' : 'secondary'}>
                      {provider.canProvideFor ? 'Active' : 'Inactive'}
                    </Badge>
                  </div>
                ))}
              </div>
            </CardContent>
          </Card>
        </TabsContent>

        <TabsContent value="operations">
          <div className="grid gap-4 md:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle>Seed Data</CardTitle>
                <CardDescription>
                  Initialize database with environment-appropriate data
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <Button
                    onClick={validateEnvironment}
                    variant="outline"
                    className="w-full"
                    disabled={loading}
                  >
                    <CheckCircle className="mr-2 h-4 w-4" />
                    Validate Environment
                  </Button>
                  <Button
                    onClick={() => setShowSeedDialog(true)}
                    className="w-full"
                    disabled={loading}
                  >
                    <Upload className="mr-2 h-4 w-4" />
                    Seed Data
                  </Button>
                </div>
              </CardContent>
            </Card>

            <Card>
              <CardHeader>
                <CardTitle>Cleanup Operations</CardTitle>
                <CardDescription>
                  Remove test data and clean up database
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <Button
                    onClick={() => cleanTestData(true)}
                    variant="outline"
                    className="w-full"
                    disabled={loading}
                  >
                    <Activity className="mr-2 h-4 w-4" />
                    Preview Cleanup (Dry Run)
                  </Button>
                  <Button
                    onClick={() => setShowCleanupDialog(true)}
                    variant="destructive"
                    className="w-full"
                    disabled={loading}
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Clean Test Data
                  </Button>
                </div>
              </CardContent>
            </Card>
          </div>

          {/* Results Display */}
          {seedResult && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  {seedResult.isSuccess ? (
                    <CheckCircle className="h-5 w-5 text-green-500" />
                  ) : (
                    <XCircle className="h-5 w-5 text-red-500" />
                  )}
                  <span>Seed Operation Result</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-3">
                    <div>
                      <p className="text-sm text-muted-foreground">Duration</p>
                      <p className="text-lg font-semibold">{seedResult.duration}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Created</p>
                      <p className="text-lg font-semibold text-green-600">{seedResult.totalCreated}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Skipped</p>
                      <p className="text-lg font-semibold text-yellow-600">{seedResult.totalSkipped}</p>
                    </div>
                  </div>

                  {seedResult.errorMessage && (
                    <Alert>
                      <AlertTriangle className="h-4 w-4" />
                      <AlertDescription>{seedResult.errorMessage}</AlertDescription>
                    </Alert>
                  )}

                  {seedResult.validationWarnings.length > 0 && (
                    <Alert>
                      <AlertTriangle className="h-4 w-4" />
                      <AlertDescription>
                        <div className="space-y-1">
                          <p className="font-semibold">Warnings:</p>
                          <ul className="list-disc list-inside space-y-1">
                            {seedResult.validationWarnings.map((warning, index) => (
                              <li key={index} className="text-sm">{warning}</li>
                            ))}
                          </ul>
                        </div>
                      </AlertDescription>
                    </Alert>
                  )}
                </div>
              </CardContent>
            </Card>
          )}

          {cleanupResult && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  {cleanupResult.isSuccess ? (
                    <CheckCircle className="h-5 w-5 text-green-500" />
                  ) : (
                    <XCircle className="h-5 w-5 text-red-500" />
                  )}
                  <span>Cleanup Operation Result</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  <div className="grid gap-4 md:grid-cols-3">
                    <div>
                      <p className="text-sm text-muted-foreground">Test Users Found</p>
                      <p className="text-lg font-semibold">{cleanupResult.testUsersFound}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Test Posts Found</p>
                      <p className="text-lg font-semibold">{cleanupResult.testPostsFound}</p>
                    </div>
                    <div>
                      <p className="text-sm text-muted-foreground">Duration</p>
                      <p className="text-lg font-semibold">{cleanupResult.duration}</p>
                    </div>
                  </div>

                  {!cleanupResult.isDryRun && (
                    <div className="grid gap-4 md:grid-cols-2">
                      <div>
                        <p className="text-sm text-muted-foreground">Users Removed</p>
                        <p className="text-lg font-semibold text-red-600">{cleanupResult.testUsersRemoved}</p>
                      </div>
                      <div>
                        <p className="text-sm text-muted-foreground">Posts Removed</p>
                        <p className="text-lg font-semibold text-red-600">{cleanupResult.testPostsRemoved}</p>
                      </div>
                    </div>
                  )}

                  {cleanupResult.isDryRun && (
                    <Alert>
                      <AlertTriangle className="h-4 w-4" />
                      <AlertDescription>
                        This was a dry run. No data was actually removed.
                      </AlertDescription>
                    </Alert>
                  )}
                </div>
              </CardContent>
            </Card>
          )}
        </TabsContent>

        <TabsContent value="history">
          <Card>
            <CardHeader>
              <CardTitle>Operation History</CardTitle>
              <CardDescription>
                Recent seed data operations and their results
              </CardDescription>
            </CardHeader>
            <CardContent>
              <div className="text-center py-8 text-muted-foreground">
                <Clock className="h-12 w-12 mx-auto mb-4" />
                <p>No recent operations to display</p>
              </div>
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>

      {/* Seed Data Dialog */}
      <Dialog open={showSeedDialog} onOpenChange={setShowSeedDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Seed Data Operation</DialogTitle>
            <DialogDescription>
              This will initialize the database with {environment} environment data.
              {environment === 'Production' && (
                <span className="text-red-600 font-semibold">
                  {' '}⚠️ This is a production environment!
                </span>
              )}
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <Alert>
              <AlertTriangle className="h-4 w-4" />
              <AlertDescription>
                Please confirm that you want to proceed with seeding data in the{' '}
                <strong>{environment}</strong> environment.
              </AlertDescription>
            </Alert>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowSeedDialog(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => seedData(false, environment === 'Production')}
              disabled={loading}
            >
              {loading && <RefreshCw className="mr-2 h-4 w-4 animate-spin" />}
              Confirm Seed Data
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Cleanup Dialog */}
      <Dialog open={showCleanupDialog} onOpenChange={setShowCleanupDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Confirm Test Data Cleanup</DialogTitle>
            <DialogDescription>
              This will permanently remove test data from the database.
              This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowCleanupDialog(false)}>
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => cleanTestData(false)}
              disabled={loading}
            >
              {loading && <RefreshCw className="mr-2 h-4 w-4 animate-spin" />}
              Clean Test Data
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default SeedDataManagement;