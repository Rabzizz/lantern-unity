using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// Minimal, editor-only client for Lantern's read + write API. Fetches all translations as CSV
    /// (a <c>key</c> column plus one column per locale) for import, and pushes edits back a locale
    /// at a time via the write API's upsert endpoint.
    /// </summary>
    internal static class LanternClient
    {
        /// <summary>Aggregate counts returned by <c>PUT …/translations</c>.</summary>
        public struct PushResult
        {
            public int Updated;
            public int Created;
            public int Skipped;
        }

        [Serializable]
        private class PushRequest
        {
            public string locale;
            public LanternEntry[] entries;
            public bool createMissingKeys;
        }

        [Serializable]
        private struct PushResponse
        {
            public int updated;
            public int created;
            public int skipped;
        }

        [Serializable]
        private struct ErrorResponse
        {
            public string error;
        }

        /// <summary>
        /// Calls <c>GET {baseUrl}/api/v1/projects/{slug}/translations?format=csv</c> with a
        /// <c>Bearer</c> token. Invokes <paramref name="onDone"/> with (csv, error) once complete —
        /// exactly one argument is non-null. Runs asynchronously without blocking the editor.
        /// </summary>
        public static void FetchCsv(string baseUrl, string slug, string token, Action<string, string> onDone)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) { onDone(null, "Base URL is empty."); return; }
            if (string.IsNullOrWhiteSpace(slug)) { onDone(null, "Project slug is empty."); return; }
            if (string.IsNullOrWhiteSpace(token)) { onDone(null, "API token is empty."); return; }

            var url = baseUrl.TrimEnd('/') + "/api/v1/projects/" +
                      UnityWebRequest.EscapeURL(slug.Trim()) + "/translations?format=csv";

            var request = UnityWebRequest.Get(url);
            request.SetRequestHeader("Authorization", "Bearer " + token.Trim());

            var operation = request.SendWebRequest();

            void Poll()
            {
                if (!operation.isDone) return;
                EditorApplication.update -= Poll;
                try
                {
                    if (IsError(request))
                        onDone(null, DescribeError(request));
                    else
                        onDone(request.downloadHandler.text, null);
                }
                finally
                {
                    request.Dispose();
                }
            }

            EditorApplication.update += Poll;
        }

        /// <summary>
        /// Calls <c>PUT {baseUrl}/api/v1/projects/{slug}/translations</c> with a <c>Bearer</c> token,
        /// upserting <paramref name="entries"/> for one <paramref name="locale"/>. The endpoint is an
        /// upsert keyed by the dotted key path: existing keys are reused (never duplicated), and
        /// missing keys are created only when <paramref name="createMissingKeys"/> is set and the
        /// token carries the <c>key:create</c> scope. Invokes <paramref name="onDone"/> with
        /// (result, error) once complete — exactly one argument is non-null. Runs asynchronously
        /// without blocking the editor.
        /// </summary>
        public static void PushTranslations(
            string baseUrl, string slug, string token,
            string locale, LanternEntry[] entries, bool createMissingKeys,
            Action<PushResult?, string> onDone)
        {
            if (string.IsNullOrWhiteSpace(baseUrl)) { onDone(null, "Base URL is empty."); return; }
            if (string.IsNullOrWhiteSpace(slug)) { onDone(null, "Project slug is empty."); return; }
            if (string.IsNullOrWhiteSpace(token)) { onDone(null, "API token is empty."); return; }
            if (string.IsNullOrWhiteSpace(locale)) { onDone(null, "Locale is empty."); return; }

            var url = baseUrl.TrimEnd('/') + "/api/v1/projects/" +
                      UnityWebRequest.EscapeURL(slug.Trim()) + "/translations";

            var payload = new PushRequest
            {
                locale = locale.Trim(),
                entries = entries ?? Array.Empty<LanternEntry>(),
                createMissingKeys = createMissingKeys,
            };
            var json = JsonUtility.ToJson(payload);

            var request = UnityWebRequest.Put(url, json);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token.Trim());

            var operation = request.SendWebRequest();

            void Poll()
            {
                if (!operation.isDone) return;
                EditorApplication.update -= Poll;
                try
                {
                    if (IsError(request))
                    {
                        onDone(null, DescribeWriteError(request));
                        return;
                    }

                    var parsed = JsonUtility.FromJson<PushResponse>(request.downloadHandler.text);
                    onDone(
                        new PushResult
                        {
                            Updated = parsed.updated,
                            Created = parsed.created,
                            Skipped = parsed.skipped,
                        },
                        null);
                }
                finally
                {
                    request.Dispose();
                }
            }

            EditorApplication.update += Poll;
        }

        private static bool IsError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result != UnityWebRequest.Result.Success;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        private static string DescribeError(UnityWebRequest request)
        {
            switch (request.responseCode)
            {
                case 401: return "401 Unauthorized — the API token is missing or invalid.";
                case 403: return "403 Forbidden — the token doesn't match this project slug.";
                case 400: return "400 Bad Request — unknown locale or bad format." + Body(request);
                case 404: return "404 Not Found — no project with that slug.";
                default:
                    var code = request.responseCode > 0 ? request.responseCode + " " : string.Empty;
                    return code + request.error + Body(request);
            }
        }

        /// <summary>
        /// Error mapping for the write path. Unlike a read, a 403 here means the token lacks the
        /// required write scope (a read-only token), not a slug mismatch; a 400 usually carries the
        /// server's reason (e.g. an unknown locale the token can't auto-create). Surfaces the
        /// server's <c>{ "error" }</c> message when present.
        /// </summary>
        private static string DescribeWriteError(UnityWebRequest request)
        {
            var detail = ErrorDetail(request);
            switch (request.responseCode)
            {
                case 401:
                    return "401 Unauthorized — the API token is missing or invalid.";
                case 403:
                    return "403 Forbidden — this token can't write to that project. Use a token with " +
                           "the translation:edit scope (create/rename also need key:create; a new locale " +
                           "needs language:manage). Read-only tokens can pull but not push." + detail;
                case 400:
                    return "400 Bad Request — " + (string.IsNullOrEmpty(detail)
                        ? "unknown locale or bad request."
                        : detail.Trim());
                case 404:
                    return "404 Not Found — no project with that slug." + detail;
                default:
                    var code = request.responseCode > 0 ? request.responseCode + " " : string.Empty;
                    return code + request.error + detail;
            }
        }

        private static string Body(UnityWebRequest request)
        {
            var text = request.downloadHandler != null ? request.downloadHandler.text : null;
            return string.IsNullOrEmpty(text) ? string.Empty : " (" + text.Trim() + ")";
        }

        /// <summary>The server's <c>{ "error": "…" }</c> message parenthesized, or empty.</summary>
        private static string ErrorDetail(UnityWebRequest request)
        {
            var text = request.downloadHandler != null ? request.downloadHandler.text : null;
            if (string.IsNullOrEmpty(text)) return string.Empty;
            try
            {
                var parsed = JsonUtility.FromJson<ErrorResponse>(text);
                if (!string.IsNullOrEmpty(parsed.error)) return " (" + parsed.error + ")";
            }
            catch
            {
                // Not JSON — fall through to the raw body.
            }
            return " (" + text.Trim() + ")";
        }
    }
}
