/**
 * 富文本渲染器组件
 * 支持Markdown渲染、代码高亮、安全过滤等
 * 完整实现marked v16 API
 */

import React from 'react';
import { marked, Tokens, RendererObject, MarkedExtension } from 'marked';
import type { Config as DOMPurifyConfig } from 'dompurify';

// Define token types for table rendering
interface TableRowToken {
  tokens?: Tokens.Generic[];
}

interface TaskListToken {
  checked: boolean;
}
import { markedHighlight } from 'marked-highlight';
import DOMPurify from 'dompurify';
import hljs from 'highlight.js';
import 'highlight.js/styles/github.css';

interface RichTextRendererProps {
  content: string;
  className?: string;
  enableSyntaxHighlight?: boolean;
  enableMath?: boolean;
  allowedTags?: string[];
  maxLength?: number;
  enableTables?: boolean;
  enableTaskLists?: boolean;
  enableLinkify?: boolean;
  sanitizeOptions?: DOMPurifyConfig;
}

/**
 * 富文本渲染器组件
 */
export default function RichTextRenderer({
  content,
  className = '',
  enableSyntaxHighlight = true,
  enableMath: _enableMath = false,
  allowedTags = [
    'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
    'p', 'br', 'strong', 'em', 'u', 's', 'del',
    'a', 'img', 'video', 'audio',
    'ul', 'ol', 'li',
    'blockquote', 'code', 'pre',
    'table', 'thead', 'tbody', 'tr', 'th', 'td',
    'div', 'span',
    'hr', 'input'
  ],
  maxLength,
  enableTables = true,
  enableTaskLists = true,
  enableLinkify: _enableLinkify = true,
  sanitizeOptions
}: RichTextRendererProps) {

  // 创建自定义渲染器
  const createCustomRenderer = React.useCallback((): Partial<RendererObject> => {
    return {
      // 自定义链接渲染（安全性）
      link(token: Tokens.Link): string {
        const { href, title, tokens } = token;
        const text = this.parser?.parseInline(tokens) || '';
        const isExternal = href && (href.startsWith('http://') || href.startsWith('https://'));
        const target = isExternal ? ' target="_blank"' : '';
        const rel = isExternal ? ' rel="noopener noreferrer"' : '';
        const titleAttr = title ? ` title="${title}"` : '';
        return `<a href="${href}"${target}${rel}${titleAttr}>${text}</a>`;
      },

      // 自定义图片渲染（延迟加载）
      image(token: Tokens.Image): string {
        const { href, title, text } = token;
        const titleAttr = title ? ` title="${title}"` : '';
        const altAttr = text ? ` alt="${text}"` : '';
        return `<img src="${href}" loading="lazy"${titleAttr}${altAttr} class="max-w-full h-auto rounded-lg shadow-sm" />`;
      },

      // 自定义代码块渲染
      code(token: Tokens.Code): string {
        const { text: code, lang: language } = token;
        if (!enableSyntaxHighlight || !language) {
          return `<pre><code class="language-${language || 'text'}">${code}</code></pre>`;
        }

        try {
          if (hljs.getLanguage(language)) {
            const highlighted = hljs.highlight(code, { language }).value;
            return `<div class="code-block relative">
              <div class="code-header bg-gray-100 px-4 py-2 text-sm text-gray-600 border-b">
                <span class="code-language font-mono">${language}</span>
              </div>
              <pre class="hljs p-4 overflow-x-auto"><code class="language-${language}">${highlighted}</code></pre>
            </div>`;
          }
        } catch (error) {
          console.warn('Syntax highlighting failed:', error);
        }

        const autoDetected = hljs.highlightAuto(code);
        return `<div class="code-block relative">
          <div class="code-header bg-gray-100 px-4 py-2 text-sm text-gray-600 border-b">
            <span class="code-language font-mono">auto</span>
          </div>
          <pre class="hljs p-4 overflow-x-auto"><code>${autoDetected.value}</code></pre>
        </div>`;
      },

      // 自定义表格渲染
      table(token: Tokens.Table): string {
        if (!enableTables) {
          return '';
        }

        let header = '';
        let body = '';

        // 处理表头
        if (token.header && token.header.length > 0) {
          const headerRow = token.header.map((cell: { tokens: Tokens.Generic[] }) => {
            const text = this.parser?.parseInline(cell.tokens) || '';
            return `<th class="px-4 py-2 bg-gray-50 font-semibold text-left">${text}</th>`;
          }).join('');
          header = `<thead><tr>${headerRow}</tr></thead>`;
        }

        // 处理表体
        if (token.rows && token.rows.length > 0) {
          const bodyRows = token.rows.map((row: TableRowToken[]) => {
            const cells = row.map((cell: TableRowToken) => {
              const text = this.parser?.parseInline((cell.tokens as Tokens.Generic[]) || []) || '';
              return `<td class="px-4 py-2 border-b">${text}</td>`;
            }).join('');
            return `<tr>${cells}</tr>`;
          }).join('');
          body = `<tbody>${bodyRows}</tbody>`;
        }

        return `<div class="table-responsive overflow-x-auto">
          <table class="table-auto w-full border-collapse border border-gray-200">
            ${header}
            ${body}
          </table>
        </div>`;
      },

      // 自定义列表项渲染（支持任务列表）
      listitem(token: Tokens.ListItem): string {
        let text = this.parser?.parseInline(token.tokens) || '';

        // 检查是否是任务列表项
        if (enableTaskLists && token.task !== undefined) {
          const checked = token.checked ? 'checked' : '';
          text = `<input type="checkbox" ${checked} disabled class="task-list-item-checkbox mr-2" /> ${text}`;
          return `<li class="task-list-item list-none">${text}</li>`;
        }

        return `<li>${text}</li>`;
      }
    };
  }, [enableSyntaxHighlight, enableTables, enableTaskLists]);

  // 创建任务列表扩展
  const createTaskListExtension = React.useCallback((): MarkedExtension => {
    if (!enableTaskLists) {
      return {};
    }

    return {
      extensions: [
        {
          name: 'taskList',
          level: 'inline',
          start(src: string): number | undefined {
            const match = src.match(/^\[([x ])\]\s/);
            return match ? match.index : undefined;
          },
          tokenizer(src: string) {
            const rule = /^\[([x ])\]\s/;
            const match = rule.exec(src);
            if (match) {
              return {
                type: 'taskList',
                raw: match[0],
                checked: match[1] === 'x'
              };
            }
          },
          renderer(token: unknown): string {
            return `<input type="checkbox" ${(token as TaskListToken).checked ? 'checked' : ''} disabled class="task-list-item-checkbox mr-2" /> `;
          }
        }
      ]
    };
  }, [enableTaskLists]);

  // 处理内容
  const processedContent = React.useMemo(() => {
    if (!content || content.trim() === '') {
      return '';
    }

    let processed = content;

    // 应用长度限制
    if (maxLength && processed.length > maxLength) {
      processed = processed.substring(0, maxLength);
      // 尝试在单词边界截断
      const lastSpace = processed.lastIndexOf(' ');
      if (lastSpace > maxLength * 0.8) {
        processed = processed.substring(0, lastSpace);
      }
      processed += '...';
    }

    try {
      // 创建marked实例的配置
      const extensions: MarkedExtension[] = [];

      // 添加语法高亮扩展
      if (enableSyntaxHighlight) {
        extensions.push(markedHighlight({
          langPrefix: 'hljs language-',
          highlight(code: string, lang: string): string {
            try {
              if (lang && hljs.getLanguage(lang)) {
                return hljs.highlight(code, { language: lang }).value;
              }
              return hljs.highlightAuto(code).value;
            } catch (error) {
              return code;
            }
          }
        }));
      }

      // 添加任务列表扩展
      const taskListExt = createTaskListExtension();
      if (taskListExt.extensions) {
        extensions.push(taskListExt);
      }

      // 配置marked选项
      marked.use({
        pedantic: false,
        gfm: true,
        breaks: true,
        renderer: createCustomRenderer()
      });

      // 应用扩展
      if (extensions.length > 0) {
        marked.use(...extensions);
      }

      // 使用marked解析Markdown
      const htmlContent = marked.parse(processed, {
        async: false,
        gfm: true,
        breaks: true
      }) as string;

      // 配置DOMPurify选项
      const purifyConfig: DOMPurifyConfig = {
        ALLOWED_TAGS: allowedTags,
        ALLOWED_ATTR: [
          'href', 'title', 'alt', 'src', 'class', 'id',
          'target', 'rel', 'loading', 'width', 'height',
          'colspan', 'rowspan', 'type', 'checked', 'disabled'
        ],
        ALLOWED_URI_REGEXP: /^(?:(?:(?:f|ht)tps?|mailto|tel|callto|sms|cid|xmpp):|[^a-z]|[a-z+.-]+(?:[^a-z+.-:]|$))/i,
        ADD_ATTR: ['target', 'rel'],
        FORBID_CONTENTS: ['script', 'style'],
        FORBID_TAGS: ['script', 'style', 'iframe', 'object', 'embed', 'form', 'textarea', 'select', 'button'],
        KEEP_CONTENT: true,
        ...sanitizeOptions
      };

      // 使用DOMPurify清理HTML
      const cleanHtml = DOMPurify.sanitize(htmlContent, purifyConfig as DOMPurifyConfig);

      return cleanHtml;
    } catch (error) {
      console.error('Markdown parsing error:', error);
      // 回退到简单的HTML转义
      return processed
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#x27;')
        .replace(/\n/g, '<br />');
    }
  }, [content, maxLength, allowedTags, enableSyntaxHighlight, sanitizeOptions, createCustomRenderer, createTaskListExtension]);

  return (
    <div
      className={`rich-text-renderer prose prose-lg max-w-none ${className}`}
      dangerouslySetInnerHTML={{ __html: processedContent }}
      style={{
        '--code-bg': '#f8f9fa',
        '--code-border': '#e9ecef',
        '--highlight-bg': '#fff3cd',
        '--link-color': '#007bff',
        '--quote-border': '#dee2e6',
        '--table-border': '#dee2e6'
      } as React.CSSProperties}
    />
  );
}