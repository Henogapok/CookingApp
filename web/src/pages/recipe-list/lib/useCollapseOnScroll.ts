import { useEffect, useState } from 'react';

/**
 * true, когда страницу прокрутили вниз, — для сворачивающейся шапки. Пороги разные (гистерезис):
 * сворачиваемся после 24px, разворачиваемся только у самого верха — иначе на границе шапка дёргается туда-обратно.
 */
export function useCollapseOnScroll(collapseAfter = 24, expandBefore = 8): boolean {
  const [collapsed, setCollapsed] = useState(false);

  useEffect(() => {
    const onScroll = () => {
      const top = window.scrollY;
      setCollapsed((was) => (was ? top >= expandBefore : top > collapseAfter));
    };

    onScroll();
    // passive — обработчик не отменяет прокрутку, браузер может скроллить, не дожидаясь его.
    window.addEventListener('scroll', onScroll, { passive: true });
    return () => window.removeEventListener('scroll', onScroll);
  }, [collapseAfter, expandBefore]);

  return collapsed;
}
