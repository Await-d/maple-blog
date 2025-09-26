/**
 * UserProfilePage - Comprehensive user profile management interface
 * Provides tabbed interface for managing all aspects of user profile
 */

import React, { useState } from 'react';
import { useAuth } from '@/hooks/useAuth';
import { UserProfile } from '@/types/auth';
import { Button } from '@/components/ui/Button';
import { Input, Textarea } from '@/components/ui/Input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Badge } from '@/components/ui/badge';
import { LoadingSpinner } from '@/components/ui/LoadingSpinner';
import { cn } from '@/utils/cn';

// Form validation types
interface ValidationErrors {
  [key: string]: string[];
}

// Tab types for better type safety
type ProfileTab = 'personal' | 'settings' | 'activity' | 'security';

// Mock data for demonstration (replace with actual API calls)
const mockUserProfile: UserProfile = {
  id: '1',
  username: 'johndoe',
  email: 'john.doe@example.com',
  displayName: 'John Doe',
  bio: 'Full-stack developer passionate about React and .NET. Love writing about tech trends and sharing knowledge with the community.',
  avatar: '/api/placeholder/150/150',
  location: 'San Francisco, CA',
  website: 'https://johndoe.dev',
  socialLinks: {
    twitter: 'johndoe',
    github: 'johndoe',
    linkedin: 'john-doe-dev'
  },
  birthday: '1990-05-15',
  timezone: 'America/Los_Angeles',
  preferences: {
    language: 'en',
    theme: 'auto',
    emailNotifications: true,
    marketingEmails: false,
    profileVisibility: 'public',
    showEmail: false
  },
  stats: {
    postsCount: 42,
    commentsCount: 156,
    joinDate: '2022-03-15',
    lastLoginDate: '2024-01-15T10:30:00Z',
    totalViews: 15420
  },
  security: {
    twoFactorEnabled: false,
    lastPasswordChange: '2023-12-01',
    activeSessions: 3,
    loginHistory: [
      {
        date: '2024-01-15T10:30:00Z',
        ip: '192.168.1.100',
        device: 'Chrome on macOS',
        location: 'San Francisco, CA'
      },
      {
        date: '2024-01-14T15:20:00Z',
        ip: '192.168.1.101',
        device: 'Safari on iPhone',
        location: 'San Francisco, CA'
      },
      {
        date: '2024-01-12T09:15:00Z',
        ip: '10.0.0.50',
        device: 'Chrome on Windows',
        location: 'New York, NY'
      }
    ]
  }
};

/**
 * Personal Information Tab Component
 */
const PersonalInfoTab: React.FC<{
  profile: UserProfile;
  onUpdate: (data: Partial<UserProfile>) => void;
  loading: boolean;
}> = ({ profile, onUpdate, loading }) => {
  const [formData, setFormData] = useState({
    displayName: profile.displayName,
    bio: profile.bio || '',
    location: profile.location || '',
    website: profile.website || '',
    birthday: profile.birthday || '',
    timezone: profile.timezone,
    twitter: profile.socialLinks.twitter || '',
    github: profile.socialLinks.github || '',
    linkedin: profile.socialLinks.linkedin || ''
  });

  const [errors, setErrors] = useState<ValidationErrors>({});
  const [avatarPreview, setAvatarPreview] = useState<string>(profile.avatar || '');
  const [isDirty, setIsDirty] = useState(false);

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    setIsDirty(true);
    // Clear error for this field
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: [] }));
    }
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        setErrors(prev => ({ ...prev, avatar: ['File size must be less than 5MB'] }));
        return;
      }
      
      const reader = new FileReader();
      reader.onload = (e) => {
        const result = e.target?.result as string;
        setAvatarPreview(result);
        setIsDirty(true);
      };
      reader.readAsDataURL(file);
    }
  };

  const validateForm = (): boolean => {
    const newErrors: ValidationErrors = {};

    if (!formData.displayName.trim()) {
      newErrors.displayName = ['Display name is required'];
    }

    if (formData.website && !formData.website.match(/^https?:\/\/.+/)) {
      newErrors.website = ['Please enter a valid URL starting with http:// or https://'];
    }

    if (formData.bio && formData.bio.length > 500) {
      newErrors.bio = ['Bio must be less than 500 characters'];
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSave = () => {
    if (!validateForm()) return;

    const updatedData: Partial<UserProfile> = {
      displayName: formData.displayName,
      bio: formData.bio,
      location: formData.location,
      website: formData.website,
      birthday: formData.birthday,
      timezone: formData.timezone,
      avatar: avatarPreview,
      socialLinks: {
        twitter: formData.twitter,
        github: formData.github,
        linkedin: formData.linkedin
      }
    };

    onUpdate(updatedData);
    setIsDirty(false);
  };

  return (
    <div className="space-y-8">
      {/* Avatar Section */}
      <Card>
        <CardHeader>
          <CardTitle>Profile Picture</CardTitle>
          <CardDescription>
            Upload a profile picture to personalize your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex items-center space-x-6">
            <div className="relative">
              <img
                src={avatarPreview || '/api/placeholder/80/80'}
                alt="Avatar preview"
                className="w-20 h-20 rounded-full object-cover border-2 border-gray-200"
              />
              <div className="absolute inset-0 rounded-full bg-black bg-opacity-0 hover:bg-opacity-20 transition-all cursor-pointer flex items-center justify-center">
                <span className="text-white opacity-0 hover:opacity-100 text-xs">Change</span>
              </div>
            </div>
            <div className="space-y-2">
              <div>
                <input
                  type="file"
                  accept="image/*"
                  onChange={handleAvatarUpload}
                  className="hidden"
                  id="avatar-upload"
                />
                <label htmlFor="avatar-upload">
                  <Button variant="outline" as="span" className="cursor-pointer">
                    Choose Photo
                  </Button>
                </label>
              </div>
              <p className="text-sm text-gray-500">
                JPG, PNG, GIF up to 5MB
              </p>
              {errors.avatar && (
                <p className="text-sm text-red-600">{errors.avatar[0]}</p>
              )}
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Basic Information */}
      <Card>
        <CardHeader>
          <CardTitle>Basic Information</CardTitle>
          <CardDescription>
            Update your basic profile information
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Input
              label="Display Name"
              value={formData.displayName}
              onChange={(e) => handleInputChange('displayName', e.target.value)}
              errorMessage={errors.displayName?.[0]}
              required
            />
            <Input
              label="Username"
              value={profile.username}
              disabled
              helperText="Username cannot be changed"
            />
          </div>

          <Input
            label="Email"
            type="email"
            value={profile.email}
            disabled
            helperText="Email cannot be changed here. Use Account Settings to update."
          />

          <Textarea
            label="Bio"
            value={formData.bio}
            onChange={(e) => handleInputChange('bio', e.target.value)}
            placeholder="Tell us about yourself..."
            rows={4}
            errorMessage={errors.bio?.[0]}
            helperText={`${formData.bio.length}/500 characters`}
          />

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Input
              label="Location"
              value={formData.location}
              onChange={(e) => handleInputChange('location', e.target.value)}
              placeholder="City, State/Country"
            />
            <Input
              label="Website"
              type="url"
              value={formData.website}
              onChange={(e) => handleInputChange('website', e.target.value)}
              placeholder="https://yourwebsite.com"
              errorMessage={errors.website?.[0]}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Input
              label="Birthday"
              type="date"
              value={formData.birthday}
              onChange={(e) => handleInputChange('birthday', e.target.value)}
            />
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Timezone
              </label>
              <select
                value={formData.timezone}
                onChange={(e) => handleInputChange('timezone', e.target.value)}
                className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
              >
                <option value="America/Los_Angeles">Pacific Time (PT)</option>
                <option value="America/Denver">Mountain Time (MT)</option>
                <option value="America/Chicago">Central Time (CT)</option>
                <option value="America/New_York">Eastern Time (ET)</option>
                <option value="UTC">UTC</option>
                <option value="Europe/London">London (GMT)</option>
                <option value="Europe/Paris">Paris (CET)</option>
                <option value="Asia/Tokyo">Tokyo (JST)</option>
              </select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Social Links */}
      <Card>
        <CardHeader>
          <CardTitle>Social Links</CardTitle>
          <CardDescription>
            Connect your social media profiles
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <Input
              label="Twitter"
              value={formData.twitter}
              onChange={(e) => handleInputChange('twitter', e.target.value)}
              placeholder="username (without @)"
              leftIcon={<span className="text-blue-400">🐦</span>}
            />
            <Input
              label="GitHub"
              value={formData.github}
              onChange={(e) => handleInputChange('github', e.target.value)}
              placeholder="username"
              leftIcon={<span>⚡</span>}
            />
          </div>
          <Input
            label="LinkedIn"
            value={formData.linkedin}
            onChange={(e) => handleInputChange('linkedin', e.target.value)}
            placeholder="profile-name"
            leftIcon={<span className="text-blue-600">💼</span>}
          />
        </CardContent>
      </Card>

      {/* Save Button */}
      <div className="flex justify-end">
        <Button
          onClick={handleSave}
          disabled={!isDirty || loading}
          loading={loading}
        >
          Save Changes
        </Button>
      </div>
    </div>
  );
};

/**
 * Account Settings Tab Component
 */
const AccountSettingsTab: React.FC<{
  profile: UserProfile;
  onUpdate: (data: Partial<UserProfile>) => void;
  loading: boolean;
}> = ({ profile, onUpdate, loading: _loading }) => {
  const [preferences, setPreferences] = useState(profile.preferences);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showChangePassword, setShowChangePassword] = useState(false);
  const [passwordData, setPasswordData] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });
  const [passwordErrors, setPasswordErrors] = useState<ValidationErrors>({});

  const handlePreferenceChange = (key: keyof typeof preferences, value: boolean | string) => {
    const updated = { ...preferences, [key]: value };
    setPreferences(updated);
    onUpdate({ preferences: updated });
  };

  const handlePasswordChange = () => {
    const errors: ValidationErrors = {};

    if (!passwordData.currentPassword) {
      errors.currentPassword = ['Current password is required'];
    }

    if (!passwordData.newPassword) {
      errors.newPassword = ['New password is required'];
    } else if (passwordData.newPassword.length < 8) {
      errors.newPassword = ['Password must be at least 8 characters'];
    }

    if (passwordData.newPassword !== passwordData.confirmPassword) {
      errors.confirmPassword = ['Passwords do not match'];
    }

    setPasswordErrors(errors);

    if (Object.keys(errors).length === 0) {
      // Handle password change API call here
      setShowChangePassword(false);
      setPasswordData({ currentPassword: '', newPassword: '', confirmPassword: '' });
    }
  };

  return (
    <div className="space-y-8">
      {/* Email & Notifications */}
      <Card>
        <CardHeader>
          <CardTitle>Email & Notifications</CardTitle>
          <CardDescription>
            Manage your email preferences and notifications
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium">Email Notifications</h4>
              <p className="text-sm text-gray-500">Receive notifications about your account activity</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={preferences.emailNotifications}
                onChange={(e) => handlePreferenceChange('emailNotifications', e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium">Marketing Emails</h4>
              <p className="text-sm text-gray-500">Receive newsletters and promotional content</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={preferences.marketingEmails}
                onChange={(e) => handlePreferenceChange('marketingEmails', e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
            </label>
          </div>
        </CardContent>
      </Card>

      {/* Privacy Settings */}
      <Card>
        <CardHeader>
          <CardTitle>Privacy Settings</CardTitle>
          <CardDescription>
            Control who can see your profile and information
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Profile Visibility
            </label>
            <select
              value={preferences.profileVisibility}
              onChange={(e) => handlePreferenceChange('profileVisibility', e.target.value as 'public' | 'private')}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="public">Public - Anyone can view your profile</option>
              <option value="private">Private - Only you can view your profile</option>
            </select>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium">Show Email Address</h4>
              <p className="text-sm text-gray-500">Display your email on your public profile</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                checked={preferences.showEmail}
                onChange={(e) => handlePreferenceChange('showEmail', e.target.checked)}
                className="sr-only peer"
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600"></div>
            </label>
          </div>
        </CardContent>
      </Card>

      {/* Appearance */}
      <Card>
        <CardHeader>
          <CardTitle>Appearance</CardTitle>
          <CardDescription>
            Customize how the site looks for you
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Theme
            </label>
            <div className="grid grid-cols-3 gap-4">
              {(['light', 'dark', 'auto'] as const).map((theme) => (
                <label
                  key={theme}
                  className={cn(
                    'flex flex-col items-center p-4 border-2 rounded-lg cursor-pointer transition-all',
                    preferences.theme === theme
                      ? 'border-blue-500 bg-blue-50'
                      : 'border-gray-200 hover:border-gray-300'
                  )}
                >
                  <input
                    type="radio"
                    name="theme"
                    value={theme}
                    checked={preferences.theme === theme}
                    onChange={(e) => handlePreferenceChange('theme', e.target.value as 'light' | 'dark' | 'auto')}
                    className="sr-only"
                  />
                  <div className="mb-2">
                    {theme === 'light' && '☀️'}
                    {theme === 'dark' && '🌙'}
                    {theme === 'auto' && '💻'}
                  </div>
                  <span className="text-sm font-medium capitalize">{theme}</span>
                </label>
              ))}
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              Language
            </label>
            <select
              value={preferences.language}
              onChange={(e) => handlePreferenceChange('language', e.target.value)}
              className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
            >
              <option value="en">English</option>
              <option value="zh">中文</option>
              <option value="es">Español</option>
              <option value="fr">Français</option>
              <option value="de">Deutsch</option>
              <option value="ja">日本語</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Security Actions */}
      <Card>
        <CardHeader>
          <CardTitle>Security</CardTitle>
          <CardDescription>
            Manage your account security settings
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <Button
            variant="outline"
            onClick={() => setShowChangePassword(true)}
            className="w-full sm:w-auto"
          >
            Change Password
          </Button>

          <div className="pt-4 border-t">
            <h4 className="font-medium text-red-600 mb-2">Danger Zone</h4>
            <p className="text-sm text-gray-500 mb-4">
              Once you delete your account, there is no going back. Please be certain.
            </p>
            <Button
              variant="destructive"
              onClick={() => setShowDeleteConfirm(true)}
              className="w-full sm:w-auto"
            >
              Delete Account
            </Button>
          </div>
        </CardContent>
      </Card>

      {/* Change Password Dialog */}
      <Dialog open={showChangePassword} onOpenChange={setShowChangePassword}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Change Password</DialogTitle>
            <DialogDescription>
              Enter your current password and choose a new one.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4">
            <Input
              label="Current Password"
              type="password"
              value={passwordData.currentPassword}
              onChange={(e) => setPasswordData(prev => ({ ...prev, currentPassword: e.target.value }))}
              errorMessage={passwordErrors.currentPassword?.[0]}
              showPasswordToggle
            />
            <Input
              label="New Password"
              type="password"
              value={passwordData.newPassword}
              onChange={(e) => setPasswordData(prev => ({ ...prev, newPassword: e.target.value }))}
              errorMessage={passwordErrors.newPassword?.[0]}
              showPasswordToggle
              helperText="Must be at least 8 characters long"
            />
            <Input
              label="Confirm New Password"
              type="password"
              value={passwordData.confirmPassword}
              onChange={(e) => setPasswordData(prev => ({ ...prev, confirmPassword: e.target.value }))}
              errorMessage={passwordErrors.confirmPassword?.[0]}
              showPasswordToggle
            />
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowChangePassword(false)}
            >
              Cancel
            </Button>
            <Button onClick={handlePasswordChange}>
              Update Password
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete Account Dialog */}
      <Dialog open={showDeleteConfirm} onOpenChange={setShowDeleteConfirm}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle className="text-red-600">Delete Account</DialogTitle>
            <DialogDescription>
              This action cannot be undone. This will permanently delete your account and remove all your data from our servers.
            </DialogDescription>
          </DialogHeader>
          <div className="bg-red-50 border border-red-200 rounded-md p-4">
            <p className="text-sm text-red-800">
              <strong>Warning:</strong> All your posts, comments, and personal data will be permanently deleted.
            </p>
          </div>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeleteConfirm(false)}
            >
              Cancel
            </Button>
            <Button
              variant="destructive"
              onClick={() => {
                // Handle account deletion
                setShowDeleteConfirm(false);
              }}
            >
              Delete Account
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

/**
 * Activity & Stats Tab Component
 */
const ActivityStatsTab: React.FC<{ profile: UserProfile }> = ({ profile }) => {
  const { stats } = profile;
  
  const activityData = [
    { date: '2024-01-15', activity: 'Published "Understanding React 19 Features"', type: 'post' },
    { date: '2024-01-14', activity: 'Commented on "Modern CSS Techniques"', type: 'comment' },
    { date: '2024-01-13', activity: 'Updated profile information', type: 'profile' },
    { date: '2024-01-12', activity: 'Published "TypeScript Best Practices"', type: 'post' },
    { date: '2024-01-10', activity: 'Liked 5 posts', type: 'interaction' }
  ];

  const contributionData = Array.from({ length: 365 }, (_, i) => ({
    date: new Date(Date.now() - i * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
    count: Math.floor(Math.random() * 5)
  }));

  return (
    <div className="space-y-8">
      {/* Statistics Overview */}
      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-2 bg-blue-100 rounded-lg">
                <span className="text-2xl">📝</span>
              </div>
              <div className="ml-4">
                <p className="text-2xl font-bold text-gray-900">{stats.postsCount}</p>
                <p className="text-sm text-gray-500">Posts Written</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-2 bg-green-100 rounded-lg">
                <span className="text-2xl">💬</span>
              </div>
              <div className="ml-4">
                <p className="text-2xl font-bold text-gray-900">{stats.commentsCount}</p>
                <p className="text-sm text-gray-500">Comments Made</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-2 bg-purple-100 rounded-lg">
                <span className="text-2xl">👀</span>
              </div>
              <div className="ml-4">
                <p className="text-2xl font-bold text-gray-900">{stats.totalViews.toLocaleString()}</p>
                <p className="text-sm text-gray-500">Total Views</p>
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent className="p-6">
            <div className="flex items-center">
              <div className="p-2 bg-orange-100 rounded-lg">
                <span className="text-2xl">📅</span>
              </div>
              <div className="ml-4">
                <p className="text-2xl font-bold text-gray-900">
                  {Math.floor((Date.now() - new Date(stats.joinDate).getTime()) / (1000 * 60 * 60 * 24))}
                </p>
                <p className="text-sm text-gray-500">Days Active</p>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>

      {/* Contribution Calendar */}
      <Card>
        <CardHeader>
          <CardTitle>Activity Heatmap</CardTitle>
          <CardDescription>
            Your daily contribution activity over the past year
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="overflow-x-auto">
            <div className="grid grid-cols-53 gap-1 min-w-max">
              {contributionData.slice(0, 371).map((day, index) => (
                <div
                  key={index}
                  className={cn(
                    'w-3 h-3 rounded-sm',
                    day.count === 0 && 'bg-gray-100',
                    day.count === 1 && 'bg-green-200',
                    day.count === 2 && 'bg-green-300',
                    day.count === 3 && 'bg-green-400',
                    day.count >= 4 && 'bg-green-500'
                  )}
                  title={`${day.date}: ${day.count} contributions`}
                />
              ))}
            </div>
          </div>
          <div className="flex items-center justify-between mt-4 text-sm text-gray-500">
            <span>Less</span>
            <div className="flex items-center space-x-1">
              <div className="w-3 h-3 bg-gray-100 rounded-sm" />
              <div className="w-3 h-3 bg-green-200 rounded-sm" />
              <div className="w-3 h-3 bg-green-300 rounded-sm" />
              <div className="w-3 h-3 bg-green-400 rounded-sm" />
              <div className="w-3 h-3 bg-green-500 rounded-sm" />
            </div>
            <span>More</span>
          </div>
        </CardContent>
      </Card>

      {/* Recent Activity */}
      <Card>
        <CardHeader>
          <CardTitle>Recent Activity</CardTitle>
          <CardDescription>
            Your recent actions and interactions
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {activityData.map((item, index) => (
              <div key={index} className="flex items-start space-x-4 p-4 border rounded-lg">
                <div className="flex-shrink-0">
                  {item.type === 'post' && <span className="text-blue-500">📝</span>}
                  {item.type === 'comment' && <span className="text-green-500">💬</span>}
                  {item.type === 'profile' && <span className="text-purple-500">👤</span>}
                  {item.type === 'interaction' && <span className="text-orange-500">❤️</span>}
                </div>
                <div className="flex-1">
                  <p className="text-sm text-gray-900">{item.activity}</p>
                  <p className="text-xs text-gray-500">{new Date(item.date).toLocaleDateString()}</p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Account Timeline */}
      <Card>
        <CardHeader>
          <CardTitle>Account Information</CardTitle>
          <CardDescription>
            Key dates and milestones for your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex justify-between items-center py-2 border-b">
              <span className="font-medium">Member Since</span>
              <span className="text-gray-600">
                {new Date(stats.joinDate).toLocaleDateString('en-US', {
                  year: 'numeric',
                  month: 'long',
                  day: 'numeric'
                })}
              </span>
            </div>
            <div className="flex justify-between items-center py-2 border-b">
              <span className="font-medium">Last Login</span>
              <span className="text-gray-600">
                {new Date(stats.lastLoginDate).toLocaleString()}
              </span>
            </div>
            <div className="flex justify-between items-center py-2">
              <span className="font-medium">Profile Visibility</span>
              <Badge variant={profile.preferences.profileVisibility === 'public' ? 'default' : 'secondary'}>
                {profile.preferences.profileVisibility}
              </Badge>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

/**
 * Security Tab Component
 */
const SecurityTab: React.FC<{ profile: UserProfile }> = ({ profile }) => {
  const { security } = profile;
  const [showSessionDetails, setShowSessionDetails] = useState(false);

  const handleEnableTwoFactor = () => {
    // Handle 2FA setup - implement actual 2FA logic here
    // This would typically involve API calls to enable/disable 2FA
  };

  const handleTerminateSession = (sessionId: string) => {
    // Handle session termination - implement actual session termination logic here
    // This would typically involve API calls to terminate specific sessions
    void sessionId; // Acknowledge parameter usage
  };

  return (
    <div className="space-y-8">
      {/* Password Security */}
      <Card>
        <CardHeader>
          <CardTitle>Password Security</CardTitle>
          <CardDescription>
            Manage your password and login security
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium">Password</h4>
              <p className="text-sm text-gray-500">
                Last changed: {new Date(security.lastPasswordChange).toLocaleDateString()}
              </p>
            </div>
            <Button variant="outline">
              Change Password
            </Button>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="font-medium">Two-Factor Authentication</h4>
              <p className="text-sm text-gray-500">
                {security.twoFactorEnabled 
                  ? 'Additional security enabled' 
                  : 'Add an extra layer of security to your account'
                }
              </p>
            </div>
            <div className="flex items-center space-x-2">
              <Badge variant={security.twoFactorEnabled ? 'default' : 'secondary'}>
                {security.twoFactorEnabled ? 'Enabled' : 'Disabled'}
              </Badge>
              <Button
                variant={security.twoFactorEnabled ? 'destructive' : 'default'}
                onClick={handleEnableTwoFactor}
              >
                {security.twoFactorEnabled ? 'Disable' : 'Enable'} 2FA
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Active Sessions */}
      <Card>
        <CardHeader>
          <CardTitle>Active Sessions</CardTitle>
          <CardDescription>
            Manage devices that are currently signed in to your account
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            <div className="flex items-center justify-between p-4 border rounded-lg bg-blue-50 border-blue-200">
              <div>
                <h4 className="font-medium text-blue-900">Current Session</h4>
                <p className="text-sm text-blue-700">
                  {security.loginHistory[0]?.device} • {security.loginHistory[0]?.location}
                </p>
                <p className="text-xs text-blue-600">
                  Last active: {new Date(security.loginHistory[0]?.date).toLocaleString()}
                </p>
              </div>
              <Badge variant="default">Current</Badge>
            </div>

            <div className="flex items-center justify-between">
              <span className="font-medium">
                Total Active Sessions: {security.activeSessions}
              </span>
              <Button
                variant="outline"
                onClick={() => setShowSessionDetails(!showSessionDetails)}
              >
                {showSessionDetails ? 'Hide' : 'Show'} Details
              </Button>
            </div>

            {showSessionDetails && (
              <div className="space-y-3">
                {security.loginHistory.slice(1).map((session, index) => (
                  <div key={index} className="flex items-center justify-between p-4 border rounded-lg">
                    <div>
                      <h4 className="font-medium">{session.device}</h4>
                      <p className="text-sm text-gray-600">
                        IP: {session.ip} • {session.location}
                      </p>
                      <p className="text-xs text-gray-500">
                        Last active: {new Date(session.date).toLocaleString()}
                      </p>
                    </div>
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleTerminateSession(session.ip)}
                    >
                      Terminate
                    </Button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </CardContent>
      </Card>

      {/* Login History */}
      <Card>
        <CardHeader>
          <CardTitle>Login History</CardTitle>
          <CardDescription>
            Recent login attempts and locations
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-3">
            {security.loginHistory.map((login, index) => (
              <div key={index} className="flex items-center justify-between p-3 border rounded-lg">
                <div className="flex items-center space-x-3">
                  <div className={cn(
                    'w-3 h-3 rounded-full',
                    index === 0 ? 'bg-green-500' : 'bg-gray-300'
                  )} />
                  <div>
                    <p className="font-medium">{login.device}</p>
                    <p className="text-sm text-gray-600">{login.location}</p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-sm">{login.ip}</p>
                  <p className="text-xs text-gray-500">
                    {new Date(login.date).toLocaleString()}
                  </p>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>

      {/* Security Recommendations */}
      <Card>
        <CardHeader>
          <CardTitle>Security Recommendations</CardTitle>
          <CardDescription>
            Suggestions to improve your account security
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {!security.twoFactorEnabled && (
              <div className="flex items-start p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
                <span className="text-yellow-500 mr-3">⚠️</span>
                <div>
                  <h4 className="font-medium text-yellow-800">Enable Two-Factor Authentication</h4>
                  <p className="text-sm text-yellow-700 mt-1">
                    Protect your account with an additional security layer.
                  </p>
                </div>
              </div>
            )}

            <div className="flex items-start p-4 bg-green-50 border border-green-200 rounded-lg">
              <span className="text-green-500 mr-3">✅</span>
              <div>
                <h4 className="font-medium text-green-800">Strong Password</h4>
                <p className="text-sm text-green-700 mt-1">
                  Your password meets our security requirements.
                </p>
              </div>
            </div>

            <div className="flex items-start p-4 bg-blue-50 border border-blue-200 rounded-lg">
              <span className="text-blue-500 mr-3">ℹ️</span>
              <div>
                <h4 className="font-medium text-blue-800">Regular Security Checkups</h4>
                <p className="text-sm text-blue-700 mt-1">
                  Review your security settings periodically to ensure your account stays secure.
                </p>
              </div>
            </div>
          </div>
        </CardContent>
      </Card>
    </div>
  );
};

/**
 * Main UserProfilePage Component
 */
const UserProfilePage: React.FC = () => {
  const { isAuthenticated, loading: authLoading } = useAuth();
  const [profile, setProfile] = useState<UserProfile>(mockUserProfile);
  const [loading, setLoading] = useState(false);
  const [activeTab, setActiveTab] = useState<ProfileTab>('personal');
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Handle profile updates
  const handleProfileUpdate = async (updatedData: Partial<UserProfile>) => {
    setLoading(true);
    try {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      setProfile(prevProfile => ({
        ...prevProfile,
        ...updatedData
      }));
      
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 3000);
    } catch (error) {
      console.error('Failed to update profile:', error);
    } finally {
      setLoading(false);
    }
  };

  // Show loading state while authenticating
  if (authLoading) {
    return (
      <div className="flex justify-center items-center min-h-96">
        <LoadingSpinner />
      </div>
    );
  }

  // Redirect if not authenticated
  if (!isAuthenticated) {
    window.location.href = '/login?redirect=/profile';
    return null;
  }

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center space-x-4 mb-4">
          <img
            src={profile.avatar || '/api/placeholder/64/64'}
            alt={profile.displayName}
            className="w-16 h-16 rounded-full object-cover border-2 border-gray-200"
          />
          <div>
            <h1 className="text-3xl font-bold text-gray-900">{profile.displayName}</h1>
            <p className="text-gray-600">@{profile.username}</p>
            {profile.bio && (
              <p className="text-gray-700 mt-1">{profile.bio}</p>
            )}
          </div>
        </div>

        {/* Success Message */}
        {saveSuccess && (
          <div className="mb-4 p-4 bg-green-50 border border-green-200 rounded-lg">
            <div className="flex items-center">
              <span className="text-green-500 mr-2">✅</span>
              <span className="text-green-800">Profile updated successfully!</span>
            </div>
          </div>
        )}
      </div>

      {/* Tabs */}
      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as ProfileTab)}>
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="personal">Personal Info</TabsTrigger>
          <TabsTrigger value="settings">Account Settings</TabsTrigger>
          <TabsTrigger value="activity">Activity & Stats</TabsTrigger>
          <TabsTrigger value="security">Security</TabsTrigger>
        </TabsList>

        <TabsContent value="personal">
          <PersonalInfoTab
            profile={profile}
            onUpdate={handleProfileUpdate}
            loading={loading}
          />
        </TabsContent>

        <TabsContent value="settings">
          <AccountSettingsTab
            profile={profile}
            onUpdate={handleProfileUpdate}
            loading={loading}
          />
        </TabsContent>

        <TabsContent value="activity">
          <ActivityStatsTab profile={profile} />
        </TabsContent>

        <TabsContent value="security">
          <SecurityTab profile={profile} />
        </TabsContent>
      </Tabs>
    </div>
  );
};

export default UserProfilePage;