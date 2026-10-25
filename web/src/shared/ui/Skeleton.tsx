import type { CSSProperties } from 'react';
import styles from './Skeleton.module.css';

interface SkeletonProps {
  width?: CSSProperties['width'];
  height: CSSProperties['height'];
  radius?: CSSProperties['borderRadius'];
  className?: string;
}

/**
 * Серая заглушка с переливом на месте ещё не загруженного содержимого — повторяет его форму,
 * поэтому, когда данные придут, вёрстка не прыгает. Для скринридера скрыта: статус «загрузка» объявляет родитель.
 */
export function Skeleton({ width = '100%', height, radius = 8, className }: SkeletonProps) {
  return (
    <span
      className={className ? `${styles.skeleton} ${className}` : styles.skeleton}
      style={{ width, height, borderRadius: radius }}
      aria-hidden="true"
    />
  );
}
