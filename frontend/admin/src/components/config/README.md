# Configuration Management Interface

## Overview

This directory contains the comprehensive system configuration management interface for the Maple Blog admin dashboard. It provides enterprise-grade configuration management with visual editing, validation, version control, and security features.

## Components

### ConfigForm.tsx
A comprehensive form component for editing system configuration with:
- **Visual Form Builder**: Intuitive form fields for different configuration sections
- **Real-time Validation**: Client-side validation with server-side confirmation
- **Conditional Fields**: Fields that show/hide based on other field values
- **Import/Export**: JSON configuration import and export functionality
- **Preview Mode**: Live preview of configuration changes

**Features:**
- ✅ Sectioned configuration (General, Features, Content, Security, Integrations)
- ✅ Field validation with custom rules
- ✅ Tooltip help and warnings
- ✅ Form state management with dirty tracking
- ✅ Import/Export JSON configurations
- ✅ Configuration preview modal

### ConfigEditor.tsx
A powerful visual configuration editor with multiple view modes:
- **Visual Editor**: Form-based configuration editing
- **JSON Editor**: Monaco-powered JSON editing with syntax highlighting
- **Tree View**: Hierarchical configuration structure display

**Features:**
- ✅ Multi-mode editing (Visual/JSON/Tree)
- ✅ Impact analysis before applying changes
- ✅ Configuration version comparison
- ✅ Backup and restore functionality
- ✅ Template management
- ✅ Conflict resolution
- ✅ Real-time validation feedback

### useSystemConfig.ts
A comprehensive React hook for configuration management:
- **State Management**: Centralized configuration state
- **API Integration**: Full CRUD operations with validation
- **Version Control**: Configuration history and rollback
- **Template System**: Save and apply configuration templates
- **Conflict Resolution**: Handle concurrent configuration changes

**Features:**
- ✅ Configuration CRUD operations
- ✅ Real-time validation
- ✅ Version history and rollback
- ✅ Backup/restore functionality
- ✅ Template management
- ✅ Impact analysis
- ✅ Conflict detection and resolution

## Pages

### Settings.tsx
The main system settings page providing:
- **Overview Dashboard**: Configuration status and quick stats
- **Configuration Editor**: Full configuration management interface
- **History Management**: Version history and comparison tools
- **Template Library**: Pre-built configuration templates
- **Security Settings**: Access control and audit configuration

**Features:**
- ✅ Tabbed interface with Overview, Configuration, History, Templates, Security
- ✅ Real-time system statistics
- ✅ Quick action buttons
- ✅ Configuration health monitoring
- ✅ Audit log viewer
- ✅ Template gallery

## Configuration Schema

The system supports various configuration categories:

### General Settings
- Site name, description, URL
- Language and timezone
- Basic branding settings

### Feature Switches
- Comments system toggle
- User registration control
- Search functionality
- Analytics tracking
- Caching system
- Maintenance mode

### Content Settings
- Posts per page
- File upload restrictions
- Auto-save configuration
- Content management options

### Security Settings
- Session timeout
- Two-factor authentication
- Password requirements
- CAPTCHA settings
- Login attempt limits

### Third-party Integrations
- Google Analytics
- Email providers (SMTP, SendGrid, Mailgun)
- CDN configuration
- External service connections

## Security Features

### Access Control
- Role-based configuration access
- Permission-based field editing
- Approval workflow for critical changes
- Audit trail for all modifications

### Validation & Safety
- Multi-layer validation (client + server)
- Configuration impact analysis
- Rollback capabilities
- Backup before changes
- Conflict detection and resolution

### Version Control
- Automatic configuration versioning
- Change history tracking
- Diff comparison between versions
- Point-in-time restore
- Configuration branching

## Usage Examples

### Basic Configuration Editing
```tsx
import { useSystemConfig } from '@/hooks/useSystemConfig';
import ConfigForm from '@/components/config/ConfigForm';

function MyConfigPage() {
  const { currentConfig, saveConfig, validateConfig } = useSystemConfig();

  return (
    <ConfigForm
      config={currentConfig}
      onSave={saveConfig}
      onValidate={validateConfig}
    />
  );
}
```

### Advanced Configuration Editor
```tsx
import ConfigEditor from '@/components/config/ConfigEditor';

function AdvancedConfigPage() {
  return (
    <ConfigEditor
      height={800}
      defaultMode="visual"
    />
  );
}
```

### Configuration Templates
```tsx
const { applyTemplate, saveAsTemplate, templates } = useSystemConfig();

// Apply a template
await applyTemplate('production-template');

// Save current config as template
await saveAsTemplate('My Template', 'Custom configuration for XYZ');
```

## Best Practices

### Performance
- Use React.memo for expensive components
- Implement proper field validation debouncing
- Lazy load Monaco editor
- Cache configuration validation results

### Security
- Always validate configuration on server side
- Sanitize user inputs
- Implement proper permission checks
- Log all configuration changes

### User Experience
- Provide clear validation feedback
- Show impact analysis before changes
- Offer rollback options
- Display loading states appropriately

## API Integration

The configuration system integrates with the following API endpoints:

```
GET    /api/admin/system/config              # List configurations
GET    /api/admin/system/config/current      # Current active config
POST   /api/admin/system/config              # Create/update config
POST   /api/admin/system/config/validate     # Validate config
GET    /api/admin/system/config/history      # Configuration history
POST   /api/admin/system/config/rollback/:id # Rollback to version
POST   /api/admin/system/config/backup       # Create backup
POST   /api/admin/system/config/restore/:id  # Restore from backup
GET    /api/admin/system/config/templates    # List templates
POST   /api/admin/system/config/templates    # Create template
POST   /api/admin/system/config/analyze-impact # Analyze changes
```

## Testing

### Component Testing
```bash
npm test -- ConfigForm.test.tsx
npm test -- ConfigEditor.test.tsx
npm test -- useSystemConfig.test.ts
```

### E2E Testing
```bash
npm run test:e2e -- config-management.spec.ts
```

## Troubleshooting

### Common Issues

1. **Monaco Editor Not Loading**
   - Ensure @monaco-editor/react is installed
   - Check for proper import statements
   - Verify Vite configuration for Monaco

2. **Validation Errors**
   - Check network connectivity to backend
   - Verify API endpoints are available
   - Review validation rules in backend

3. **Performance Issues**
   - Enable React DevTools Profiler
   - Check for unnecessary re-renders
   - Optimize large configuration objects

## Future Enhancements

- [ ] Real-time collaborative editing
- [ ] Configuration schema evolution
- [ ] Advanced diff visualization
- [ ] Configuration testing framework
- [ ] Multi-environment sync
- [ ] Configuration as code (GitOps)
- [ ] Advanced approval workflows
- [ ] Configuration analytics

## Contributing

When adding new configuration fields:

1. Update the `SystemConfiguration` interface in `types/systemConfig.ts`
2. Add field definitions to the appropriate section in `ConfigForm.tsx`
3. Update validation rules in the backend
4. Add tests for new functionality
5. Update this documentation

## Dependencies

- React 19+ with hooks
- Ant Design 5+ for UI components
- Monaco Editor for JSON editing
- TanStack Query for state management
- Zustand for local state
- TypeScript for type safety