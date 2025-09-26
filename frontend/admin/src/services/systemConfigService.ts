import {
  SystemConfiguration,
  ConfigurationTemplate,
  ConfigurationValidationResult,
  ConfigurationDiff,
  ConfigurationBackup,
  ConfigurationConflict,
  ConflictResolution,
  ConfigurationApproval,
  ConfigurationAudit,
  ConfigurationImpactAnalysis,
  ConfigurationDeployment,
  ConfigurationSchema,
  SystemConfigResponse,
  ConfigurationListResponse,
  ConfigurationHistoryResponse,
  ConfigurationTemplateResponse,
} from '../types/systemConfig';
import apiClient from './api';

// API Error interfaces
interface ValidationError {
  field: string;
  message: string;
}

interface ApiErrorResponse {
  response?: {
    data?: {
      validationErrors?: ValidationError[];
    };
  };
}

class SystemConfigService {
  private readonly baseUrl = '/api/admin/system/config';

  // Configuration Management
  async getConfigurations(): Promise<SystemConfiguration[]> {
    try {
      const response = await apiClient.get<ConfigurationListResponse>(`${this.baseUrl}`);
      return response.data.configs;
    } catch (error) {
      console.error('Failed to fetch configurations:', error);
      throw new Error('Failed to fetch configurations');
    }
  }

  async getCurrentConfiguration(): Promise<SystemConfiguration | null> {
    try {
      const response = await apiClient.get<SystemConfigResponse>(`${this.baseUrl}/current`);
      return response.data.config;
    } catch (error) {
      console.error('Failed to fetch current configuration:', error);
      return null;
    }
  }

  async getConfiguration(id: string): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.get<SystemConfiguration>(`${this.baseUrl}/${id}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch configuration ${id}:`, error);
      throw new Error(`Failed to fetch configuration ${id}`);
    }
  }

  async saveConfiguration(config: Partial<SystemConfiguration>): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}`, config);
      return response.data;
    } catch (error: unknown) {
      console.error('Failed to save configuration:', error);
      const apiError = error as ApiErrorResponse;
      if (apiError.response?.data?.validationErrors) {
        throw {
          message: 'Validation failed',
          validationErrors: apiError.response.data.validationErrors,
        };
      }
      throw new Error('Failed to save configuration');
    }
  }

  async updateConfiguration(id: string, config: Partial<SystemConfiguration>): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.put<SystemConfiguration>(`${this.baseUrl}/${id}`, config);
      return response.data;
    } catch (error: unknown) {
      console.error(`Failed to update configuration ${id}:`, error);
      const apiError = error as ApiErrorResponse;
      if (apiError.response?.data?.validationErrors) {
        throw {
          message: 'Validation failed',
          validationErrors: apiError.response.data.validationErrors,
        };
      }
      throw new Error(`Failed to update configuration ${id}`);
    }
  }

  async deleteConfiguration(id: string): Promise<void> {
    try {
      await apiClient.delete(`${this.baseUrl}/${id}`);
    } catch (error) {
      console.error(`Failed to delete configuration ${id}:`, error);
      throw new Error(`Failed to delete configuration ${id}`);
    }
  }

  // Configuration Validation
  async validateConfiguration(config: Partial<SystemConfiguration>): Promise<ConfigurationValidationResult> {
    try {
      const response = await apiClient.post<ConfigurationValidationResult>(`${this.baseUrl}/validate`, config);
      return response.data;
    } catch (error) {
      console.error('Failed to validate configuration:', error);
      throw new Error('Failed to validate configuration');
    }
  }

  async getConfigurationSchema(): Promise<ConfigurationSchema> {
    try {
      const response = await apiClient.get<ConfigurationSchema>(`${this.baseUrl}/schema`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch configuration schema:', error);
      throw new Error('Failed to fetch configuration schema');
    }
  }

  // Version Management
  async getConfigurationHistory(): Promise<SystemConfiguration[]> {
    try {
      const response = await apiClient.get<ConfigurationHistoryResponse>(`${this.baseUrl}/history`);
      return response.data.history;
    } catch (error) {
      console.error('Failed to fetch configuration history:', error);
      throw new Error('Failed to fetch configuration history');
    }
  }

  async rollbackToVersion(versionId: string): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/rollback/${versionId}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to rollback to version ${versionId}:`, error);
      throw new Error(`Failed to rollback to version ${versionId}`);
    }
  }

  async compareVersions(versionA: string, versionB: string): Promise<ConfigurationDiff> {
    try {
      const response = await apiClient.get<ConfigurationDiff>(
        `${this.baseUrl}/compare`,
        {
          params: { versionA, versionB }
        }
      );
      return response.data;
    } catch (error) {
      console.error('Failed to compare versions:', error);
      throw new Error('Failed to compare versions');
    }
  }

  // Backup Management
  async createBackup(description: string): Promise<ConfigurationBackup> {
    try {
      const response = await apiClient.post<ConfigurationBackup>(`${this.baseUrl}/backup`, {
        description,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to create backup:', error);
      throw new Error('Failed to create backup');
    }
  }

  async getBackups(): Promise<ConfigurationBackup[]> {
    try {
      const response = await apiClient.get<ConfigurationBackup[]>(`${this.baseUrl}/backups`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch backups:', error);
      throw new Error('Failed to fetch backups');
    }
  }

  async restoreFromBackup(backupId: string): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/restore/${backupId}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to restore from backup ${backupId}:`, error);
      throw new Error(`Failed to restore from backup ${backupId}`);
    }
  }

  async deleteBackup(backupId: string): Promise<void> {
    try {
      await apiClient.delete(`${this.baseUrl}/backups/${backupId}`);
    } catch (error) {
      console.error(`Failed to delete backup ${backupId}:`, error);
      throw new Error(`Failed to delete backup ${backupId}`);
    }
  }

  // Template Management
  async getTemplates(): Promise<ConfigurationTemplate[]> {
    try {
      const response = await apiClient.get<ConfigurationTemplateResponse>(`${this.baseUrl}/templates`);
      return response.data.templates;
    } catch (error) {
      console.error('Failed to fetch templates:', error);
      throw new Error('Failed to fetch templates');
    }
  }

  async getTemplate(templateId: string): Promise<ConfigurationTemplate> {
    try {
      const response = await apiClient.get<ConfigurationTemplate>(`${this.baseUrl}/templates/${templateId}`);
      return response.data;
    } catch (error) {
      console.error(`Failed to fetch template ${templateId}:`, error);
      throw new Error(`Failed to fetch template ${templateId}`);
    }
  }

  async saveAsTemplate(name: string, description: string, config: SystemConfiguration): Promise<ConfigurationTemplate> {
    try {
      const response = await apiClient.post<ConfigurationTemplate>(`${this.baseUrl}/templates`, {
        name,
        description,
        config,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to save template:', error);
      throw new Error('Failed to save template');
    }
  }

  async updateTemplate(templateId: string, template: Partial<ConfigurationTemplate>): Promise<ConfigurationTemplate> {
    try {
      const response = await apiClient.put<ConfigurationTemplate>(`${this.baseUrl}/templates/${templateId}`, template);
      return response.data;
    } catch (error) {
      console.error(`Failed to update template ${templateId}:`, error);
      throw new Error(`Failed to update template ${templateId}`);
    }
  }

  async applyTemplate(templateId: string): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/templates/${templateId}/apply`);
      return response.data;
    } catch (error) {
      console.error(`Failed to apply template ${templateId}:`, error);
      throw new Error(`Failed to apply template ${templateId}`);
    }
  }

  async deleteTemplate(templateId: string): Promise<void> {
    try {
      await apiClient.delete(`${this.baseUrl}/templates/${templateId}`);
    } catch (error) {
      console.error(`Failed to delete template ${templateId}:`, error);
      throw new Error(`Failed to delete template ${templateId}`);
    }
  }

  // Conflict Resolution
  async getConflicts(): Promise<ConfigurationConflict[]> {
    try {
      const response = await apiClient.get<ConfigurationConflict[]>(`${this.baseUrl}/conflicts`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch conflicts:', error);
      throw new Error('Failed to fetch conflicts');
    }
  }

  async resolveConflict(conflictId: string, resolution: ConflictResolution): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/conflicts/${conflictId}/resolve`, resolution);
    } catch (error) {
      console.error(`Failed to resolve conflict ${conflictId}:`, error);
      throw new Error(`Failed to resolve conflict ${conflictId}`);
    }
  }

  // Approval Workflow
  async getApprovals(): Promise<ConfigurationApproval[]> {
    try {
      const response = await apiClient.get<ConfigurationApproval[]>(`${this.baseUrl}/approvals`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch approvals:', error);
      throw new Error('Failed to fetch approvals');
    }
  }

  async requestApproval(configId: string, changes: Partial<SystemConfiguration>, reason: string): Promise<ConfigurationApproval> {
    try {
      const response = await apiClient.post<ConfigurationApproval>(`${this.baseUrl}/approvals`, {
        configId,
        changes,
        reason,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to request approval:', error);
      throw new Error('Failed to request approval');
    }
  }

  async approveConfiguration(approvalId: string, reason?: string): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/approvals/${approvalId}/approve`, { reason });
    } catch (error) {
      console.error(`Failed to approve configuration ${approvalId}:`, error);
      throw new Error(`Failed to approve configuration ${approvalId}`);
    }
  }

  async rejectConfiguration(approvalId: string, reason: string): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/approvals/${approvalId}/reject`, { reason });
    } catch (error) {
      console.error(`Failed to reject configuration ${approvalId}:`, error);
      throw new Error(`Failed to reject configuration ${approvalId}`);
    }
  }

  // Impact Analysis
  async analyzeImpact(config: Partial<SystemConfiguration>): Promise<ConfigurationImpactAnalysis> {
    try {
      const response = await apiClient.post<ConfigurationImpactAnalysis>(`${this.baseUrl}/analyze-impact`, config);
      return response.data;
    } catch (error) {
      console.error('Failed to analyze impact:', error);
      throw new Error('Failed to analyze impact');
    }
  }

  // Audit Trail
  async getAuditLogs(configId?: string): Promise<ConfigurationAudit[]> {
    try {
      const params = configId ? { configId } : {};
      const response = await apiClient.get<ConfigurationAudit[]>(`${this.baseUrl}/audit`, { params });
      return response.data;
    } catch (error) {
      console.error('Failed to fetch audit logs:', error);
      throw new Error('Failed to fetch audit logs');
    }
  }

  // Deployment Management
  async deployConfiguration(configId: string, environment: string): Promise<ConfigurationDeployment> {
    try {
      const response = await apiClient.post<ConfigurationDeployment>(`${this.baseUrl}/deploy`, {
        configId,
        environment,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to deploy configuration:', error);
      throw new Error('Failed to deploy configuration');
    }
  }

  async getDeployments(): Promise<ConfigurationDeployment[]> {
    try {
      const response = await apiClient.get<ConfigurationDeployment[]>(`${this.baseUrl}/deployments`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch deployments:', error);
      throw new Error('Failed to fetch deployments');
    }
  }

  async rollbackDeployment(deploymentId: string, reason: string): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/deployments/${deploymentId}/rollback`, { reason });
    } catch (error) {
      console.error(`Failed to rollback deployment ${deploymentId}:`, error);
      throw new Error(`Failed to rollback deployment ${deploymentId}`);
    }
  }

  // Configuration Export/Import
  async exportConfiguration(configId: string, format: 'json' | 'yaml' = 'json'): Promise<Blob> {
    try {
      const response = await apiClient.get(`${this.baseUrl}/${configId}/export`, {
        params: { format },
        responseType: 'blob',
      });
      return response.data;
    } catch (error) {
      console.error(`Failed to export configuration ${configId}:`, error);
      throw new Error(`Failed to export configuration ${configId}`);
    }
  }

  async importConfiguration(file: File): Promise<SystemConfiguration> {
    try {
      const formData = new FormData();
      formData.append('file', file);

      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/import`, formData, {
        headers: {
          'Content-Type': 'multipart/form-data',
        },
      });
      return response.data;
    } catch (error) {
      console.error('Failed to import configuration:', error);
      throw new Error('Failed to import configuration');
    }
  }

  // Environment Management
  async syncConfigurationBetweenEnvironments(
    sourceEnv: string,
    targetEnv: string,
    configId: string
  ): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/sync`, {
        sourceEnvironment: sourceEnv,
        targetEnvironment: targetEnv,
        configId,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to sync configuration between environments:', error);
      throw new Error('Failed to sync configuration between environments');
    }
  }

  // Health and Status
  async getConfigurationHealth(): Promise<{
    status: 'healthy' | 'warning' | 'error';
    lastChecked: string;
    issues: Array<{
      severity: 'error' | 'warning' | 'info';
      message: string;
      field?: string;
    }>;
  }> {
    try {
      const response = await apiClient.get(`${this.baseUrl}/health`);
      return response.data;
    } catch (error) {
      console.error('Failed to fetch configuration health:', error);
      throw new Error('Failed to fetch configuration health');
    }
  }

  // Configuration Migration
  async migrateConfiguration(fromVersion: string, toVersion: string): Promise<SystemConfiguration> {
    try {
      const response = await apiClient.post<SystemConfiguration>(`${this.baseUrl}/migrate`, {
        fromVersion,
        toVersion,
      });
      return response.data;
    } catch (error) {
      console.error('Failed to migrate configuration:', error);
      throw new Error('Failed to migrate configuration');
    }
  }

  // Real-time Updates
  subscribeToConfigurationChanges(_callback: (config: SystemConfiguration) => void): () => void {
    // This would typically use WebSockets or Server-Sent Events
    // For now, return a no-op unsubscribe function
    return () => {};
  }

  // Cache Management
  async clearConfigurationCache(): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/cache/clear`);
    } catch (error) {
      console.error('Failed to clear configuration cache:', error);
      throw new Error('Failed to clear configuration cache');
    }
  }

  async warmupConfigurationCache(): Promise<void> {
    try {
      await apiClient.post(`${this.baseUrl}/cache/warmup`);
    } catch (error) {
      console.error('Failed to warmup configuration cache:', error);
      throw new Error('Failed to warmup configuration cache');
    }
  }
}

export const systemConfigService = new SystemConfigService();
export default systemConfigService;