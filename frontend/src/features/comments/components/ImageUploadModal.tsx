// @ts-nocheck
/**
 * 图片上传模态框组件
 * 支持拖拽上传、粘贴上传、URL导入
 */

import React, { useState, useRef, useCallback, useEffect } from 'react';
import { uploadApi } from '../../../services/uploadApi';

interface ImageUploadModalProps {
  onImageInsert: (imageUrl: string, alt?: string) => void;
  onClose: () => void;
  className?: string;
}

const ImageUploadModal: React.FC<ImageUploadModalProps> = ({
  onImageInsert,
  onClose,
  className = ''
}) => {
  const [uploadMethod, setUploadMethod] = useState<'upload' | 'url'>('upload');
  const [imageUrl, setImageUrl] = useState('');
  const [altText, setAltText] = useState('');
  const [uploading, setUploading] = useState(false);
  const [dragActive, setDragActive] = useState(false);
  const [previewUrl, setPreviewUrl] = useState('');

  const fileInputRef = useRef<HTMLInputElement>(null);
  const modalRef = useRef<HTMLDivElement>(null);

  // 点击外部关闭
  useEffect(() => {
    const handleClickOutside = (e: MouseEvent) => {
      if (modalRef.current && !modalRef.current.contains(e.target as Node)) {
        onClose();
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, [onClose]);

  // ESC键关闭
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        onClose();
      }
    };

    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [onClose]);

  // 文件上传处理
  const handleFileUpload = useCallback(async (files: FileList) => {
    const file = files[0];
    if (!file) return;

    // 验证文件类型
    if (!file.type.startsWith('image/')) {
      alert('请选择图片文件');
      return;
    }

    // 验证文件大小 (5MB)
    const maxSize = 5 * 1024 * 1024;
    if (file.size > maxSize) {
      alert('图片大小不能超过 5MB');
      return;
    }

    setUploading(true);

    try {
      // 上传图片
      const uploadResult = await uploadApi.uploadImage(file);

      // 设置预览
      setPreviewUrl(uploadResult.url);
      setImageUrl(uploadResult.url);

      // 自动填充alt文本
      if (!altText && uploadResult.filename) {
        setAltText(uploadResult.filename.replace(/\.[^/.]+$/, ''));
      }

    } catch (error) {
      console.error('Image upload failed:', error);
      alert('图片上传失败，请重试');
    } finally {
      setUploading(false);
    }
  }, [altText]);

  // 拖拽处理
  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();

    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);

    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      handleFileUpload(e.dataTransfer.files);
    }
  }, [handleFileUpload]);

  // 文件选择处理
  const handleFileInputChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      handleFileUpload(e.target.files);
    }
  }, [handleFileUpload]);

  // URL输入处理
  const handleUrlChange = useCallback((url: string) => {
    setImageUrl(url);

    // 简单的URL验证和预览
    if (url && (url.startsWith('http://') || url.startsWith('https://'))) {
      setPreviewUrl(url);
    } else {
      setPreviewUrl('');
    }
  }, []);

  // 提交处理
  const handleSubmit = useCallback((e: React.FormEvent) => {
    e.preventDefault();

    if (!imageUrl.trim()) {
      alert('请上传图片或输入图片URL');
      return;
    }

    onImageInsert(imageUrl, altText.trim());
  }, [imageUrl, altText, onImageInsert]);

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black bg-opacity-50">
      <div
        ref={modalRef}
        className={`bg-white dark:bg-gray-800 rounded-lg shadow-xl w-full max-w-md ${className}`}
      >
        <div className="p-6">
          {/* 标题 */}
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">
              插入图片
            </h3>
            <button
              onClick={onClose}
              className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-300"
            >
              <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          {/* 上传方式切换 */}
          <div className="flex border-b border-gray-200 dark:border-gray-700 mb-6">
            <button
              onClick={() => setUploadMethod('upload')}
              className={`flex-1 px-3 py-2 text-sm font-medium text-center border-b-2 transition-colors ${
                uploadMethod === 'upload'
                  ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
              }`}
            >
              本地上传
            </button>
            <button
              onClick={() => setUploadMethod('url')}
              className={`flex-1 px-3 py-2 text-sm font-medium text-center border-b-2 transition-colors ${
                uploadMethod === 'url'
                  ? 'border-blue-500 text-blue-600 dark:text-blue-400'
                  : 'border-transparent text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-300'
              }`}
            >
              链接导入
            </button>
          </div>

          <form onSubmit={handleSubmit} className="space-y-4">
            {/* 本地上传 */}
            {uploadMethod === 'upload' && (
              <div>
                <div
                  className={`
                    relative border-2 border-dashed rounded-lg p-6 text-center transition-colors
                    ${dragActive
                      ? 'border-blue-400 bg-blue-50 dark:bg-blue-900/20'
                      : 'border-gray-300 dark:border-gray-600 hover:border-gray-400 dark:hover:border-gray-500'
                    }
                  `}
                  onDragEnter={handleDrag}
                  onDragLeave={handleDrag}
                  onDragOver={handleDrag}
                  onDrop={handleDrop}
                >
                  <input
                    ref={fileInputRef}
                    type="file"
                    accept="image/*"
                    onChange={handleFileInputChange}
                    className="hidden"
                  />

                  {uploading ? (
                    <div className="flex flex-col items-center">
                      <svg className="w-8 h-8 animate-spin text-blue-500 mb-2" fill="none" viewBox="0 0 24 24">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                      </svg>
                      <p className="text-sm text-gray-600 dark:text-gray-400">上传中...</p>
                    </div>
                  ) : (
                    <div className="flex flex-col items-center">
                      <svg className="w-12 h-12 text-gray-400 mb-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
                      </svg>
                      <p className="text-sm text-gray-600 dark:text-gray-300 mb-2">
                        拖拽图片到这里或
                        <button
                          type="button"
                          onClick={() => fileInputRef.current?.click()}
                          className="text-blue-600 dark:text-blue-400 underline ml-1"
                        >
                          点击上传
                        </button>
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400">
                        支持 JPG、PNG、GIF，最大 5MB
                      </p>
                    </div>
                  )}
                </div>
              </div>
            )}

            {/* URL导入 */}
            {uploadMethod === 'url' && (
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  图片URL
                </label>
                <input
                  type="url"
                  value={imageUrl}
                  onChange={(e) => handleUrlChange(e.target.value)}
                  placeholder="https://example.com/image.jpg"
                  className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md
                             bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100
                             focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                  required
                />
              </div>
            )}

            {/* Alt文本 */}
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                图片描述 (可选)
              </label>
              <input
                type="text"
                value={altText}
                onChange={(e) => setAltText(e.target.value)}
                placeholder="描述图片内容"
                className="w-full px-3 py-2 border border-gray-300 dark:border-gray-600 rounded-md
                           bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100
                           focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              />
            </div>

            {/* 预览 */}
            {previewUrl && (
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  预览
                </label>
                <div className="border border-gray-200 dark:border-gray-700 rounded-lg overflow-hidden">
                  <img
                    src={previewUrl}
                    alt={altText || '预览图片'}
                    className="w-full max-h-48 object-contain bg-gray-50 dark:bg-gray-900"
                    onError={() => setPreviewUrl('')}
                  />
                </div>
              </div>
            )}

            {/* 操作按钮 */}
            <div className="flex justify-end space-x-3 pt-4">
              <button
                type="button"
                onClick={onClose}
                className="px-4 py-2 text-sm text-gray-700 dark:text-gray-300 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
              >
                取消
              </button>
              <button
                type="submit"
                disabled={!imageUrl.trim() || uploading}
                className="px-4 py-2 text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 disabled:bg-gray-400 disabled:cursor-not-allowed rounded-lg transition-colors"
              >
                {uploading ? '上传中...' : '插入图片'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default ImageUploadModal;