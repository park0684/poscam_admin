(function () {
    "use strict";

    const activeRequests = new Map();
    let beforeUnloadRegistered = false;

    function registerBeforeUnload() {
        if (beforeUnloadRegistered) {
            return;
        }

        window.addEventListener("beforeunload", handleBeforeUnload);
        beforeUnloadRegistered = true;
    }

    function unregisterBeforeUnloadIfIdle() {
        if (!beforeUnloadRegistered || activeRequests.size > 0) {
            return;
        }

        window.removeEventListener("beforeunload", handleBeforeUnload);
        beforeUnloadRegistered = false;
    }

    function handleBeforeUnload(event) {
        if (activeRequests.size === 0) {
            return;
        }

        event.preventDefault();
        event.returnValue = "";
    }

    function safeInvoke(dotNetReference, methodName, payload) {
        if (!dotNetReference) {
            return;
        }

        dotNetReference
            .invokeMethodAsync(methodName, payload)
            .catch(function () {
                // Blazor circuit가 종료된 경우 callback 실패는 무시한다.
            });
    }

    function getInput(inputElementId) {
        if (!inputElementId) {
            return null;
        }

        return document.getElementById(inputElementId);
    }

    function getSelectedFile(inputElementId) {
        const input = getInput(inputElementId);

        if (!input || !input.files || input.files.length === 0) {
            return null;
        }

        return input.files[0];
    }

    function tryParseJson(value) {
        if (!value || typeof value !== "string") {
            return null;
        }

        try {
            return JSON.parse(value);
        } catch (_) {
            return null;
        }
    }

    function getHeader(xhr, name) {
        try {
            return xhr.getResponseHeader(name);
        } catch (_) {
            return null;
        }
    }

    function completeRequest(uploadKey, result) {
        const state = activeRequests.get(uploadKey);

        if (!state || state.completed) {
            return;
        }

        state.completed = true;
        activeRequests.delete(uploadKey);
        unregisterBeforeUnloadIfIdle();
        safeInvoke(state.dotNetReference, "OnUploadCompletedAsync", result);
    }

    function createServerResult(xhr) {
        const payload = tryParseJson(xhr.responseText);
        const data = payload && payload.data ? payload.data : null;
        const success = xhr.status >= 200
            && xhr.status < 300
            && payload
            && payload.success === true
            && data;

        return {
            success: !!success,
            cancelled: false,
            networkError: false,
            httpStatus: xhr.status || 0,
            errorCode: payload && Number.isInteger(payload.errorCode)
                ? payload.errorCode
                : 0,
            message: payload && typeof payload.message === "string"
                ? payload.message
                : "",
            requestId: getHeader(xhr, "X-Request-ID"),
            artifactCode: data && Number.isFinite(data.artifactCode)
                ? data.artifactCode
                : null,
            fileName: data && typeof data.fileName === "string"
                ? data.fileName
                : null,
            fileSize: data && Number.isFinite(data.fileSize)
                ? data.fileSize
                : null,
            sha256: data && typeof data.sha256 === "string"
                ? data.sha256
                : null,
            replaced: !!(data && data.replaced === true)
        };
    }

    window.poscamUpdateUpload = {
        getSelectedFileInfo: function (inputElementId) {
            const file = getSelectedFile(inputElementId);

            if (!file) {
                return {
                    hasFile: false,
                    name: "",
                    size: 0,
                    type: ""
                };
            }

            return {
                hasFile: true,
                name: file.name || "",
                size: Number.isFinite(file.size) ? file.size : 0,
                type: file.type || ""
            };
        },

        clearSelectedFile: function (inputElementId) {
            const input = getInput(inputElementId);

            if (input) {
                input.value = "";
            }
        },

        start: function (options, dotNetReference) {
            if (!options || !options.uploadKey) {
                return {
                    started: false,
                    message: "업로드 식별값이 올바르지 않습니다."
                };
            }

            if (activeRequests.has(options.uploadKey)) {
                return {
                    started: false,
                    message: "이미 업로드가 진행 중입니다."
                };
            }

            const file = getSelectedFile(options.inputElementId);

            if (!file) {
                return {
                    started: false,
                    message: "업로드할 ZIP 파일을 선택해 주세요."
                };
            }

            if (!options.url || !options.token) {
                return {
                    started: false,
                    message: "업로드 주소 또는 로그인 정보가 없습니다."
                };
            }

            const xhr = new XMLHttpRequest();
            const formData = new FormData();

            formData.append("os", options.os || "");
            formData.append("architecture", options.architecture || "");
            formData.append("packageType", options.packageType || "");
            formData.append("file", file, file.name);

            activeRequests.set(options.uploadKey, {
                xhr: xhr,
                dotNetReference: dotNetReference,
                completed: false
            });
            registerBeforeUnload();

            xhr.open("POST", options.url, true);
            xhr.withCredentials = false;
            xhr.setRequestHeader("Accept", "application/json");
            xhr.setRequestHeader("Authorization", "Bearer " + options.token);

            if (options.requestId) {
                xhr.setRequestHeader("X-Request-ID", options.requestId);
            }

            xhr.upload.onprogress = function (event) {
                const total = event.lengthComputable ? event.total : file.size;
                const percent = total > 0
                    ? Math.min(100, Math.max(0, Math.round((event.loaded / total) * 100)))
                    : 0;

                safeInvoke(dotNetReference, "OnUploadProgressAsync", {
                    loaded: event.loaded || 0,
                    total: total || 0,
                    percent: percent
                });
            };

            xhr.onload = function () {
                completeRequest(options.uploadKey, createServerResult(xhr));
            };

            xhr.onerror = function () {
                completeRequest(options.uploadKey, {
                    success: false,
                    cancelled: false,
                    networkError: true,
                    httpStatus: xhr.status || 0,
                    errorCode: 0,
                    message: "",
                    requestId: getHeader(xhr, "X-Request-ID"),
                    artifactCode: null,
                    fileName: null,
                    fileSize: null,
                    sha256: null,
                    replaced: false
                });
            };

            xhr.onabort = function () {
                completeRequest(options.uploadKey, {
                    success: false,
                    cancelled: true,
                    networkError: false,
                    httpStatus: 0,
                    errorCode: 0,
                    message: "",
                    requestId: null,
                    artifactCode: null,
                    fileName: null,
                    fileSize: null,
                    sha256: null,
                    replaced: false
                });
            };

            xhr.ontimeout = function () {
                completeRequest(options.uploadKey, {
                    success: false,
                    cancelled: false,
                    networkError: true,
                    httpStatus: 0,
                    errorCode: 0,
                    message: "",
                    requestId: null,
                    artifactCode: null,
                    fileName: null,
                    fileSize: null,
                    sha256: null,
                    replaced: false
                });
            };

            try {
                xhr.send(formData);
            } catch (_) {
                activeRequests.delete(options.uploadKey);
                unregisterBeforeUnloadIfIdle();

                return {
                    started: false,
                    message: "브라우저에서 업로드를 시작하지 못했습니다."
                };
            }

            return {
                started: true,
                message: ""
            };
        },

        cancel: function (uploadKey) {
            const state = activeRequests.get(uploadKey);

            if (!state || !state.xhr) {
                return false;
            }

            state.xhr.abort();
            return true;
        },

        isUploading: function (uploadKey) {
            return activeRequests.has(uploadKey);
        }
    };
})();
