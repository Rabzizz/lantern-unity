using System;
using UnityEditor;
using UnityEngine.Networking;

namespace Lantern.Unity.Editor
{
    /// <summary>
    /// Minimal, editor-only client for Lantern's read API. Fetches all translations as CSV
    /// (a <c>key</c> column plus one column per locale) so they can be imported straight into a
    /// Unity Localization String Table Collection.
    /// </summary>
    internal static class LanternClient
    {
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

        private static string Body(UnityWebRequest request)
        {
            var text = request.downloadHandler != null ? request.downloadHandler.text : null;
            return string.IsNullOrEmpty(text) ? string.Empty : " (" + text.Trim() + ")";
        }
    }
}
