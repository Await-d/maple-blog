// 提供静态message的替代方案，用于向后兼容
// 注意：这仍然不是最佳实践，建议在组件中直接使用 useMessage hook

import { message as antdMessage } from 'antd';

// 为了向后兼容，保留静态方法但添加警告
export const message = {
  ...antdMessage,
  success: (content: string) => {
    console.warn('使用静态message方法，建议使用useMessage hook');
    return antdMessage.success(content);
  },
  error: (content: string) => {
    console.warn('使用静态message方法，建议使用useMessage hook');
    return antdMessage.error(content);
  },
  info: (content: string) => {
    console.warn('使用静态message方法，建议使用useMessage hook');
    return antdMessage.info(content);
  },
  warning: (content: string) => {
    console.warn('使用静态message方法，建议使用useMessage hook');
    return antdMessage.warning(content);
  },
  loading: (content: string) => {
    console.warn('使用静态message方法，建议使用useMessage hook');
    return antdMessage.loading(content);
  },
};

export default message;