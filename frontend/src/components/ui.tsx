import type {
  ButtonHTMLAttributes,
  InputHTMLAttributes,
  ReactNode,
  SelectHTMLAttributes,
} from "react";

/** Projenin tasarım dili — tüm ekranlarda aynı bileşenler kullanılır. */

export function cx(...parts: (string | false | null | undefined)[]) {
  return parts.filter(Boolean).join(" ");
}

// ---------------------------------------------------------------- Kart

export function Card({
  children,
  className,
}: {
  children: ReactNode;
  className?: string;
}) {
  return (
    <section
      className={cx(
        "rounded-xl border border-ink-200 bg-white shadow-sm",
        className,
      )}
    >
      {children}
    </section>
  );
}

export function CardHeader({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <header className="flex flex-wrap items-start justify-between gap-3 border-b border-ink-200 px-5 py-4">
      <div>
        <h2 className="text-sm font-semibold text-ink-900">{title}</h2>
        {description && (
          <p className="mt-0.5 text-xs text-ink-500">{description}</p>
        )}
      </div>
      {action}
    </header>
  );
}

// ---------------------------------------------------------------- Buton

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "primary" | "secondary" | "ghost" | "danger";
  size?: "sm" | "md";
};

export function Button({
  variant = "primary",
  size = "md",
  className,
  ...props
}: ButtonProps) {
  const variants = {
    primary:
      "bg-brand-600 text-white hover:bg-brand-700 disabled:bg-ink-300 disabled:text-ink-500",
    secondary:
      "border border-ink-300 bg-white text-ink-700 hover:bg-ink-50 disabled:text-ink-400",
    ghost: "text-ink-600 hover:bg-ink-100 disabled:text-ink-400",
    danger:
      "border border-red-200 bg-white text-red-600 hover:bg-red-50 disabled:text-ink-400",
  };

  const sizes = {
    sm: "px-2.5 py-1.5 text-xs",
    md: "px-3.5 py-2 text-sm",
  };

  return (
    <button
      className={cx(
        "inline-flex items-center justify-center gap-1.5 rounded-lg font-medium transition-colors",
        "focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand-600",
        "disabled:cursor-not-allowed",
        variants[variant],
        sizes[size],
        className,
      )}
      {...props}
    />
  );
}

// ---------------------------------------------------------------- Form

const fieldStyle =
  "w-full rounded-lg border border-ink-300 bg-white px-3 py-2 text-sm text-ink-900 " +
  "placeholder:text-ink-400 focus:border-brand-500 focus:outline-none focus:ring-2 focus:ring-brand-100 " +
  "disabled:bg-ink-100 disabled:text-ink-400";

export function Input({
  className,
  ...props
}: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cx(fieldStyle, className)} {...props} />;
}

export function Select({
  className,
  children,
  ...props
}: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select className={cx(fieldStyle, "pr-8", className)} {...props}>
      {children}
    </select>
  );
}

export function Field({
  label,
  hint,
  children,
}: {
  label: string;
  hint?: string;
  children: ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-medium text-ink-600">
        {label}
      </span>
      {children}
      {hint && <span className="mt-1 block text-xs text-ink-400">{hint}</span>}
    </label>
  );
}

// ---------------------------------------------------------------- Geri bildirim

export function Badge({
  children,
  tone = "neutral",
}: {
  children: ReactNode;
  tone?: "neutral" | "success" | "warning" | "danger" | "brand";
}) {
  const tones = {
    neutral: "bg-ink-100 text-ink-600 border-ink-200",
    success: "bg-emerald-50 text-emerald-700 border-emerald-200",
    warning: "bg-amber-50 text-amber-700 border-amber-200",
    danger: "bg-red-50 text-red-700 border-red-200",
    brand: "bg-brand-50 text-brand-700 border-brand-200",
  };

  return (
    <span
      className={cx(
        "inline-flex items-center rounded-md border px-2 py-0.5 text-xs font-medium",
        tones[tone],
      )}
    >
      {children}
    </span>
  );
}

export function Alert({
  tone = "info",
  children,
}: {
  tone?: "info" | "error" | "success" | "warning";
  children: ReactNode;
}) {
  const tones = {
    info: "border-brand-200 bg-brand-50 text-brand-700",
    error: "border-red-200 bg-red-50 text-red-700",
    success: "border-emerald-200 bg-emerald-50 text-emerald-700",
    warning: "border-amber-200 bg-amber-50 text-amber-800",
  };

  return (
    <div className={cx("rounded-lg border px-3.5 py-2.5 text-sm", tones[tone])}>
      {children}
    </div>
  );
}

export function EmptyState({
  title,
  description,
  action,
}: {
  title: string;
  description?: string;
  action?: ReactNode;
}) {
  return (
    <div className="flex flex-col items-center gap-2 px-6 py-14 text-center">
      <p className="text-sm font-medium text-ink-700">{title}</p>
      {description && (
        <p className="max-w-sm text-xs text-ink-500">{description}</p>
      )}
      {action && <div className="mt-2">{action}</div>}
    </div>
  );
}

export function Stat({
  label,
  value,
  sub,
  tone = "neutral",
}: {
  label: string;
  value: string | number;
  sub?: string;
  tone?: "neutral" | "success" | "warning" | "danger";
}) {
  const tones = {
    neutral: "text-ink-900",
    success: "text-emerald-600",
    warning: "text-amber-600",
    danger: "text-red-600",
  };

  return (
    <div className="rounded-xl border border-ink-200 bg-white px-4 py-3.5">
      <p className="text-xs font-medium tracking-wide text-ink-500 uppercase">
        {label}
      </p>
      <p className={cx("mt-1 text-2xl font-semibold tabular-nums", tones[tone])}>
        {value}
      </p>
      {sub && <p className="mt-0.5 text-xs text-ink-400">{sub}</p>}
    </div>
  );
}

export function Spinner({ label }: { label?: string }) {
  return (
    <div className="flex items-center gap-2 text-sm text-ink-500">
      <span className="size-4 animate-spin rounded-full border-2 border-ink-300 border-t-brand-600" />
      {label}
    </div>
  );
}
