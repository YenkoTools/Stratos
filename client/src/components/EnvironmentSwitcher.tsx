import { useState } from 'react';

const ENVIRONMENTS = ['dev', 'qa', 'stage', 'prod'] as const;
type Environment = typeof ENVIRONMENTS[number];

export default function EnvironmentSwitcher() {
  const [active, setActive] = useState<Environment>('dev');
  const [switching, setSwitching] = useState(false);

  const switchEnv = async (env: Environment) => {
    if (env === 'prod') {
      const confirmed = window.confirm(
        'You are switching to PRODUCTION. Are you sure?'
      );
      if (!confirmed) return;
    }

    setSwitching(true);
    try {
      const res = await fetch(`/internal/environment/${env}`, {
        method: 'POST',
      });
      if (res.ok) {
        setActive(env);
        window.dispatchEvent(
          new CustomEvent('env-changed', { detail: env })
        );
      }
    } finally {
      setSwitching(false);
    }
  };

  return (
    <div className="flex items-center gap-2">
      {ENVIRONMENTS.map((env) => (
        <button
          key={env}
          onClick={() => switchEnv(env)}
          disabled={switching}
          className={`px-4 py-1.5 rounded text-sm font-medium uppercase tracking-wide transition-colors
            ${active === env
              ? env === 'prod'
                ? 'bg-red-600 text-white'
                : 'bg-blue-600 text-white'
              : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
        >
          {env}
        </button>
      ))}
    </div>
  );
}
