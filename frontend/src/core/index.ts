type PathQuery = Record<string, string | number | boolean | null | undefined>;

/** Posts JSON and rejects HTTP failures so mutation error handlers receive them. */
export async function postWithPathQuery<T>(
  path: string,
  pathquery: PathQuery | undefined,
  data: unknown,
): Promise<T> {
  const query = new URLSearchParams();
  Object.entries(pathquery ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null) query.set(key, String(value));
  });
  const suffix = query.toString();
  const response = await fetch(suffix ? `${path}${path.includes('?') ? '&' : '?'}${suffix}` : path, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(data),
  });
  const body = await response.json().catch(() => null);
  if (!response.ok) {
    const problemMessage = [body?.title, body?.detail]
      .filter(value => typeof value === 'string' && value.trim()).join(': ');
    const message = typeof body?.message === 'string' && body.message.trim() ? body.message : problemMessage;
    throw new Error(message || `Request failed (${response.status})`);
  }
  return body as T;
}
