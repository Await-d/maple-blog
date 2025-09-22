// @ts-nocheck
import { useState, useCallback, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { systemConfigService } from '../services/systemConfigService';
import { SystemConfiguration, ConfigurationDiff, ConfigurationValidationError } from '../types/systemConfig';

interface UseSystemConfigReturn {
  // Data state
  configs: SystemConfiguration[];
  currentConfig: SystemConfiguration | null;
  configHistory: SystemConfiguration[];
  validationErrors: ConfigurationValidationError[];

  // Loading states
  isLoading: boolean;
  isSaving: boolean;
  isValidating: boolean;

  // Operations
  loadConfigs: () => Promise<void>;
  saveConfig: (config: Partial<SystemConfiguration>) => Promise<void>;
  validateConfig: (config: Partial<SystemConfiguration>) => Promise<boolean>;
  rollbackToVersion: (versionId: string) => Promise<void>;
  createBackup: (description: string) => Promise<void>;
  restoreFromBackup: (backupId: string) => Promise<void>;
  compareVersions: (versionA: string, versionB: string) => Promise<ConfigurationDiff>;

  // Template operations
  applyTemplate: (templateId: string) => Promise<void>;
  saveAsTemplate: (name: string, description: string) => Promise<void>;

  // Conflict resolution
  resolveConflict: (conflictId: string, resolution: any) => Promise<void>;

  // Impact analysis
  analyzeImpact: (config: Partial<SystemConfiguration>) => Promise<any>;
}

export const useSystemConfig = (): UseSystemConfigReturn => {
  const queryClient = useQueryClient();
  const [validationErrors, setValidationErrors] = useState<ConfigurationValidationError[]>([]);
  const [currentConfig, setCurrentConfig] = useState<SystemConfiguration | null>(null);

  // Query for configurations
  const {
    data: configs = [],
    isLoading
  } = useQuery({
    queryKey: ['systemConfigs'],
    queryFn: systemConfigService.getConfigurations,
    staleTime: 5 * 60 * 1000, // 5 minutes
  });

  // Query for configuration history
  const {
    data: configHistory = []
  } = useQuery({
    queryKey: ['configHistory'],
    queryFn: systemConfigService.getConfigurationHistory,
    staleTime: 10 * 60 * 1000, // 10 minutes
  });

  // Load current active configuration
  useEffect(() => {
    const activeConfig = configs.find(config => config.isActive);
    setCurrentConfig(activeConfig || null);
  }, [configs]);

  // Save configuration mutation
  const saveConfigMutation = useMutation({
    mutationFn: systemConfigService.saveConfiguration,
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
      queryClient.invalidateQueries({ queryKey: ['configHistory'] });
      message.success('Configuration saved successfully');
      setValidationErrors([]);
    },
    onError: (error: any) => {
      console.error('Failed to save configuration:', error);
      if (error.validationErrors) {
        setValidationErrors(error.validationErrors);
      }
      message.error('Failed to save configuration');
    },
  });

  // Validation mutation
  const validateConfigMutation = useMutation({
    mutationFn: systemConfigService.validateConfiguration,
    onSuccess: (data) => {
      setValidationErrors(data.errors || []);
      if (data.isValid) {
        message.success('Configuration is valid');
      } else {
        message.warning('Configuration has validation issues');
      }
    },
    onError: (error) => {
      console.error('Failed to validate configuration:', error);
      message.error('Failed to validate configuration');
    },
  });

  // Rollback mutation
  const rollbackMutation = useMutation({
    mutationFn: systemConfigService.rollbackToVersion,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
      queryClient.invalidateQueries({ queryKey: ['configHistory'] });
      message.success('Configuration rolled back successfully');
    },
    onError: (error) => {
      console.error('Failed to rollback configuration:', error);
      message.error('Failed to rollback configuration');
    },
  });

  // Backup creation mutation
  const createBackupMutation = useMutation({
    mutationFn: ({ description }: { description: string }) =>
      systemConfigService.createBackup(description),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['configHistory'] });
      message.success('Backup created successfully');
    },
    onError: (error) => {
      console.error('Failed to create backup:', error);
      message.error('Failed to create backup');
    },
  });

  // Restore from backup mutation
  const restoreBackupMutation = useMutation({
    mutationFn: systemConfigService.restoreFromBackup,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
      queryClient.invalidateQueries({ queryKey: ['configHistory'] });
      message.success('Configuration restored from backup');
    },
    onError: (error) => {
      console.error('Failed to restore from backup:', error);
      message.error('Failed to restore from backup');
    },
  });

  // Apply template mutation
  const applyTemplateMutation = useMutation({
    mutationFn: systemConfigService.applyTemplate,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
      message.success('Template applied successfully');
    },
    onError: (error) => {
      console.error('Failed to apply template:', error);
      message.error('Failed to apply template');
    },
  });

  // Save as template mutation
  const saveTemplateMutation = useMutation({
    mutationFn: ({ name, description }: { name: string; description: string }) =>
      systemConfigService.saveAsTemplate(name, description, currentConfig!),
    onSuccess: () => {
      message.success('Template saved successfully');
    },
    onError: (error) => {
      console.error('Failed to save template:', error);
      message.error('Failed to save template');
    },
  });

  // Conflict resolution mutation
  const resolveConflictMutation = useMutation({
    mutationFn: ({ conflictId, resolution }: { conflictId: string; resolution: any }) =>
      systemConfigService.resolveConflict(conflictId, resolution),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
      message.success('Conflict resolved successfully');
    },
    onError: (error) => {
      console.error('Failed to resolve conflict:', error);
      message.error('Failed to resolve conflict');
    },
  });

  // Operations
  const loadConfigs = useCallback(async () => {
    await queryClient.invalidateQueries({ queryKey: ['systemConfigs'] });
  }, [queryClient]);

  const saveConfig = useCallback(async (config: Partial<SystemConfiguration>) => {
    await saveConfigMutation.mutateAsync(config);
  }, [saveConfigMutation]);

  const validateConfig = useCallback(async (config: Partial<SystemConfiguration>): Promise<boolean> => {
    const result = await validateConfigMutation.mutateAsync(config);
    return result.isValid;
  }, [validateConfigMutation]);

  const rollbackToVersion = useCallback(async (versionId: string) => {
    await rollbackMutation.mutateAsync(versionId);
  }, [rollbackMutation]);

  const createBackup = useCallback(async (description: string) => {
    await createBackupMutation.mutateAsync({ description });
  }, [createBackupMutation]);

  const restoreFromBackup = useCallback(async (backupId: string) => {
    await restoreBackupMutation.mutateAsync(backupId);
  }, [restoreBackupMutation]);

  const compareVersions = useCallback(async (versionA: string, versionB: string): Promise<ConfigurationDiff> => {
    return systemConfigService.compareVersions(versionA, versionB);
  }, []);

  const applyTemplate = useCallback(async (templateId: string) => {
    await applyTemplateMutation.mutateAsync(templateId);
  }, [applyTemplateMutation]);

  const saveAsTemplate = useCallback(async (name: string, description: string) => {
    if (!currentConfig) {
      throw new Error('No current configuration to save as template');
    }
    await saveTemplateMutation.mutateAsync({ name, description });
  }, [saveTemplateMutation, currentConfig]);

  const resolveConflict = useCallback(async (conflictId: string, resolution: any) => {
    await resolveConflictMutation.mutateAsync({ conflictId, resolution });
  }, [resolveConflictMutation]);

  const analyzeImpact = useCallback(async (config: Partial<SystemConfiguration>) => {
    return systemConfigService.analyzeImpact(config);
  }, []);

  return {
    // Data state
    configs,
    currentConfig,
    configHistory,
    validationErrors,

    // Loading states
    isLoading,
    isSaving: saveConfigMutation.isPending,
    isValidating: validateConfigMutation.isPending,

    // Operations
    loadConfigs,
    saveConfig,
    validateConfig,
    rollbackToVersion,
    createBackup,
    restoreFromBackup,
    compareVersions,

    // Template operations
    applyTemplate,
    saveAsTemplate,

    // Conflict resolution
    resolveConflict,

    // Impact analysis
    analyzeImpact,
  };
};

export default useSystemConfig;