/**
 * GlobalFooter - 全局页脚组件包装器
 * 对现有Footer组件的重新导出和适配
 */

import React from 'react';
import { Footer } from './Footer';

interface GlobalFooterProps {
  className?: string;
}

export const GlobalFooter: React.FC<GlobalFooterProps> = ({
  className,
}) => {
  return (
    <Footer
      className={className}
    />
  );
};

export default GlobalFooter;