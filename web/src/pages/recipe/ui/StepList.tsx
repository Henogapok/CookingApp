import { formatTimer, type RecipeStep } from '@/entities/recipe';
import styles from './StepList.module.css';

interface StepListProps {
  steps: RecipeStep[];
}

/** Пронумерованные шаги; у шага с таймером — его длительность (сам таймер — в будущем режиме готовки). */
export function StepList({ steps }: StepListProps) {
  return (
    <section className={styles.section}>
      <h2 className={styles.heading}>Шаги</h2>
      <ol className={styles.list}>
        {steps.map((step) => (
          <li key={step.stepNumber} className={styles.step}>
            <span className={styles.number} aria-hidden="true">
              {step.stepNumber}
            </span>
            <div className={styles.body}>
              <p className={styles.text}>{step.instruction}</p>
              {step.timerSeconds !== null && (
                <span className={styles.timer}>
                  <svg
                    aria-hidden="true"
                    width="16"
                    height="16"
                    viewBox="0 0 24 24"
                    fill="none"
                    stroke="currentColor"
                    strokeWidth="2"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  >
                    <circle cx="12" cy="13" r="8" />
                    <path d="M12 9v4l2.5 2" />
                    <path d="M9 2h6" />
                  </svg>
                  Таймер {formatTimer(step.timerSeconds)}
                </span>
              )}
            </div>
          </li>
        ))}
      </ol>
    </section>
  );
}
