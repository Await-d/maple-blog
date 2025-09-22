// @ts-nocheck
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '../../../test/utils';
import userEvent from '@testing-library/user-event';
import { MarkdownEditor } from './MarkdownEditor';

// Mock heroicons
vi.mock('@heroicons/react/24/outline', () => ({
  BoldIcon: ({ className }: { className?: string }) => <div className={className} data-testid="bold-icon" />,
  ItalicIcon: ({ className }: { className?: string }) => <div className={className} data-testid="italic-icon" />,
  LinkIcon: ({ className }: { className?: string }) => <div className={className} data-testid="link-icon" />,
  PhotoIcon: ({ className }: { className?: string }) => <div className={className} data-testid="photo-icon" />,
  CodeBracketIcon: ({ className }: { className?: string }) => <div className={className} data-testid="code-icon" />,
  ListBulletIcon: ({ className }: { className?: string }) => <div className={className} data-testid="bullet-list-icon" />,
  NumberedListIcon: ({ className }: { className?: string }) => <div className={className} data-testid="numbered-list-icon" />,
  EyeIcon: ({ className }: { className?: string }) => <div className={className} data-testid="eye-icon" />
}));

describe('MarkdownEditor', () => {
  const mockOnChange = vi.fn();
  const mockOnImageUpload = vi.fn();

  const defaultProps = {
    value: '',
    onChange: mockOnChange
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('Basic Rendering', () => {
    it('renders the editor with default props', () => {
      render(<MarkdownEditor {...defaultProps} />);

      expect(screen.getByTestId('markdown-editor')).toBeInTheDocument();
      expect(screen.getByTestId('markdown-textarea')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Write your post content in Markdown...')).toBeInTheDocument();
    });

    it('displays the current value in the textarea', () => {
      const testValue = '# Test Content\n\nThis is a test.';
      render(<MarkdownEditor {...defaultProps} value={testValue} />);

      const textarea = screen.getByTestId('markdown-textarea');
      expect(textarea).toHaveValue(testValue);
    });

    it('shows character count', () => {
      const testValue = 'Hello World';
      render(<MarkdownEditor {...defaultProps} value={testValue} />);

      expect(screen.getByText('11 characters')).toBeInTheDocument();
    });

    it('renders with custom placeholder', () => {
      const customPlaceholder = 'Custom placeholder text';
      render(<MarkdownEditor {...defaultProps} placeholder={customPlaceholder} />);

      expect(screen.getByPlaceholderText(customPlaceholder)).toBeInTheDocument();
    });

    it('applies custom height style', () => {
      render(<MarkdownEditor {...defaultProps} height="600px" />);

      const textarea = screen.getByTestId('markdown-textarea');
      expect(textarea).toHaveStyle({ height: '600px' });
    });
  });

  describe('Toolbar Functionality', () => {
    it('renders all toolbar buttons', () => {
      render(<MarkdownEditor {...defaultProps} />);

      expect(screen.getByTestId('toolbar-bold')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-italic')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-link')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-image')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-code')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-bullet-list')).toBeInTheDocument();
      expect(screen.getByTestId('toolbar-numbered-list')).toBeInTheDocument();
    });

    it('inserts bold markdown when bold button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const boldButton = screen.getByTestId('toolbar-bold');
      await user.click(boldButton);

      expect(mockOnChange).toHaveBeenCalledWith('**bold text**');
    });

    it('inserts italic markdown when italic button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const italicButton = screen.getByTestId('toolbar-italic');
      await user.click(italicButton);

      expect(mockOnChange).toHaveBeenCalledWith('*italic text*');
    });

    it('inserts link markdown when link button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const linkButton = screen.getByTestId('toolbar-link');
      await user.click(linkButton);

      expect(mockOnChange).toHaveBeenCalledWith('[link text](url)');
    });

    it('inserts code markdown when code button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const codeButton = screen.getByTestId('toolbar-code');
      await user.click(codeButton);

      expect(mockOnChange).toHaveBeenCalledWith('`code`');
    });

    it('inserts bullet list markdown when bullet list button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const bulletListButton = screen.getByTestId('toolbar-bullet-list');
      await user.click(bulletListButton);

      expect(mockOnChange).toHaveBeenCalledWith('- list item');
    });

    it('inserts numbered list markdown when numbered list button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const numberedListButton = screen.getByTestId('toolbar-numbered-list');
      await user.click(numberedListButton);

      expect(mockOnChange).toHaveBeenCalledWith('1. list item');
    });
  });

  describe('Text Selection Handling', () => {
    it('wraps selected text with bold markdown', async () => {
      const user = userEvent.setup();
      const initialValue = 'Hello World';
      render(<MarkdownEditor {...defaultProps} value={initialValue} />);

      const textarea = screen.getByTestId('markdown-textarea');

      // Simulate text selection
      fireEvent.select(textarea, { target: { selectionStart: 6, selectionEnd: 11 } });

      const boldButton = screen.getByTestId('toolbar-bold');
      await user.click(boldButton);

      expect(mockOnChange).toHaveBeenCalledWith('Hello **World**');
    });

    it('wraps selected text with italic markdown', async () => {
      const user = userEvent.setup();
      const initialValue = 'Hello World';
      render(<MarkdownEditor {...defaultProps} value={initialValue} />);

      const textarea = screen.getByTestId('markdown-textarea');
      fireEvent.select(textarea, { target: { selectionStart: 0, selectionEnd: 5 } });

      const italicButton = screen.getByTestId('toolbar-italic');
      await user.click(italicButton);

      expect(mockOnChange).toHaveBeenCalledWith('*Hello* World');
    });
  });

  describe('Keyboard Shortcuts', () => {
    it('handles Ctrl+B for bold', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.click(textarea);
      await user.keyboard('{Control>}b{/Control}');

      expect(mockOnChange).toHaveBeenCalledWith('**bold text**');
    });

    it('handles Ctrl+I for italic', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.click(textarea);
      await user.keyboard('{Control>}i{/Control}');

      expect(mockOnChange).toHaveBeenCalledWith('*italic text*');
    });

    it('handles Ctrl+K for link', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.click(textarea);
      await user.keyboard('{Control>}k{/Control}');

      expect(mockOnChange).toHaveBeenCalledWith('[link text](url)');
    });

    it('handles Tab for indentation', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.click(textarea);
      await user.keyboard('{Tab}');

      expect(mockOnChange).toHaveBeenCalledWith('  ');
    });
  });

  describe('Preview Mode', () => {
    it('shows preview toggle button when enablePreview is true', () => {
      render(<MarkdownEditor {...defaultProps} enablePreview={true} />);

      expect(screen.getByTestId('preview-toggle')).toBeInTheDocument();
      expect(screen.getByText('Preview')).toBeInTheDocument();
    });

    it('hides preview toggle button when enablePreview is false', () => {
      render(<MarkdownEditor {...defaultProps} enablePreview={false} />);

      expect(screen.queryByTestId('preview-toggle')).not.toBeInTheDocument();
    });

    it('toggles between edit and preview mode', async () => {
      const user = userEvent.setup();
      const testValue = '# Test Header\n\nTest content';
      render(<MarkdownEditor {...defaultProps} value={testValue} enablePreview={true} />);

      const previewToggle = screen.getByTestId('preview-toggle');

      // Initially in edit mode
      expect(screen.getByTestId('markdown-textarea')).toBeInTheDocument();
      expect(screen.queryByTestId('markdown-preview')).not.toBeInTheDocument();

      // Switch to preview mode
      await user.click(previewToggle);

      expect(screen.queryByTestId('markdown-textarea')).not.toBeInTheDocument();
      expect(screen.getByTestId('markdown-preview')).toBeInTheDocument();
      expect(screen.getByText('Edit')).toBeInTheDocument();

      // Switch back to edit mode
      await user.click(previewToggle);

      expect(screen.getByTestId('markdown-textarea')).toBeInTheDocument();
      expect(screen.queryByTestId('markdown-preview')).not.toBeInTheDocument();
      expect(screen.getByText('Preview')).toBeInTheDocument();
    });
  });

  describe('Image Upload', () => {
    it('shows image upload input when onImageUpload is provided', () => {
      render(<MarkdownEditor {...defaultProps} onImageUpload={mockOnImageUpload} />);

      expect(screen.getByTestId('image-upload-input')).toBeInTheDocument();
    });

    it('triggers file input when image button is clicked', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} onImageUpload={mockOnImageUpload} />);

      const imageButton = screen.getByTestId('toolbar-image');
      const fileInput = screen.getByTestId('image-upload-input');

      // Mock click on file input
      const clickSpy = vi.spyOn(fileInput, 'click');
      await user.click(imageButton);

      expect(clickSpy).toHaveBeenCalled();
    });

    it('handles successful image upload', async () => {
      mockOnImageUpload.mockResolvedValue('https://example.com/uploaded-image.jpg');

      render(<MarkdownEditor {...defaultProps} onImageUpload={mockOnImageUpload} />);

      const fileInput = screen.getByTestId('image-upload-input');
      const file = new File(['test image'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      await waitFor(() => {
        expect(mockOnImageUpload).toHaveBeenCalledWith(file);
      });

      await waitFor(() => {
        expect(mockOnChange).toHaveBeenCalledWith('![test.jpg](https://example.com/uploaded-image.jpg)');
      });
    });

    it('handles image upload error gracefully', async () => {
      const _consoleErrorSpy = vi.spyOn(console, 'error').mockImplementation(() => { /* Mock implementation */ });
      mockOnImageUpload.mockRejectedValue(new Error('Upload failed'));

      render(<MarkdownEditor {...defaultProps} onImageUpload={mockOnImageUpload} />);

      const fileInput = screen.getByTestId('image-upload-input');
      const file = new File(['test image'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      await waitFor(() => {
        expect(mockOnImageUpload).toHaveBeenCalledWith(file);
      });

      await waitFor(() => {
        expect(consoleErrorSpy).toHaveBeenCalledWith('Image upload failed:', expect.any(Error));
      });

      consoleErrorSpy.mockRestore();
    });

    it('shows uploading state during image upload', async () => {
      let resolveUpload: (value: string) => void;
      const uploadPromise = new Promise<string>((resolve) => {
        resolveUpload = resolve;
      });
      mockOnImageUpload.mockReturnValue(uploadPromise);

      render(<MarkdownEditor {...defaultProps} onImageUpload={mockOnImageUpload} />);

      const fileInput = screen.getByTestId('image-upload-input');
      const imageButton = screen.getByTestId('toolbar-image');
      const file = new File(['test image'], 'test.jpg', { type: 'image/jpeg' });

      fireEvent.change(fileInput, { target: { files: [file] } });

      // Button should be disabled and show uploading state
      await waitFor(() => {
        expect(imageButton).toBeDisabled();
        expect(imageButton).toHaveClass('animate-pulse');
      });

      // Resolve the upload
      resolveUpload!('https://example.com/uploaded-image.jpg');

      await waitFor(() => {
        expect(imageButton).not.toBeDisabled();
        expect(imageButton).not.toHaveClass('animate-pulse');
      });
    });
  });

  describe('Disabled State', () => {
    it('disables textarea when disabled prop is true', () => {
      render(<MarkdownEditor {...defaultProps} disabled={true} />);

      const textarea = screen.getByTestId('markdown-textarea');
      expect(textarea).toBeDisabled();
      expect(textarea).toHaveClass('disabled:bg-gray-50', 'disabled:cursor-not-allowed');
    });

    it('disables all toolbar buttons when disabled', () => {
      render(<MarkdownEditor {...defaultProps} disabled={true} />);

      const toolbarButtons = [
        'toolbar-bold',
        'toolbar-italic',
        'toolbar-link',
        'toolbar-image',
        'toolbar-code',
        'toolbar-bullet-list',
        'toolbar-numbered-list'
      ];

      toolbarButtons.forEach(buttonTestId => {
        const button = screen.getByTestId(buttonTestId);
        expect(button).toBeDisabled();
      });
    });

    it('prevents keyboard shortcuts when disabled', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} disabled={true} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.click(textarea);
      await user.keyboard('{Control>}b{/Control}');

      expect(mockOnChange).not.toHaveBeenCalled();
    });
  });

  describe('Text Input Handling', () => {
    it('calls onChange when user types in textarea', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.type(textarea, 'Hello World');

      expect(mockOnChange).toHaveBeenCalledWith('Hello World');
    });

    it('maintains cursor position after toolbar actions', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} />);

      const textarea = screen.getByTestId('markdown-textarea');
      await user.type(textarea, 'Before  After');

      // Position cursor between "Before" and "After"
      textarea.setSelectionRange(6, 6);

      const boldButton = screen.getByTestId('toolbar-bold');
      await user.click(boldButton);

      expect(mockOnChange).toHaveBeenCalledWith('Before **bold text** After');
    });
  });

  describe('Accessibility', () => {
    it('has proper ARIA labels and roles', () => {
      render(<MarkdownEditor {...defaultProps} />);

      const editor = screen.getByTestId('markdown-editor');
      expect(editor).toBeInTheDocument();

      // Toolbar buttons should have proper titles
      const boldButton = screen.getByTestId('toolbar-bold');
      expect(boldButton).toHaveAttribute('title', 'Bold (Ctrl+B)');
    });

    it('supports custom test id', () => {
      render(<MarkdownEditor {...defaultProps} data-testid="custom-editor" />);

      expect(screen.getByTestId('custom-editor')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('handles empty value gracefully', () => {
      render(<MarkdownEditor {...defaultProps} value="" />);

      expect(screen.getByText('0 characters')).toBeInTheDocument();
    });

    it('handles very long content', () => {
      const longContent = 'A'.repeat(10000);
      render(<MarkdownEditor {...defaultProps} value={longContent} />);

      expect(screen.getByText('10000 characters')).toBeInTheDocument();
      const textarea = screen.getByTestId('markdown-textarea');
      expect(textarea).toHaveValue(longContent);
    });

    it('handles special characters in content', () => {
      const specialContent = '# Title\n\n**Bold** *italic* `code` [link](url)\n\n- List item\n- Another item';
      render(<MarkdownEditor {...defaultProps} value={specialContent} />);

      const textarea = screen.getByTestId('markdown-textarea');
      expect(textarea).toHaveValue(specialContent);
    });

    it('shows empty preview message when content is empty', async () => {
      const user = userEvent.setup();
      render(<MarkdownEditor {...defaultProps} value="" enablePreview={true} />);

      const previewToggle = screen.getByTestId('preview-toggle');
      await user.click(previewToggle);

      expect(screen.getByText('Nothing to preview')).toBeInTheDocument();
    });
  });
});