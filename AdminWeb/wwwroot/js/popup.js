window.poscamPopup = {
    /** 
    * 신규 매장 등록 팝업
    */
    openStoreCreate: function () {
        this.openFixedPopup(
            `/stores/popup/new`,
            `store_create`,
            1420,
            720
        );
    },
    /**
     * 매장 상세 팝업
     */
    openStoreDetail: function (storeCode) {
        this.openFixedPopup(
            `/stores/popup/${storeCode}`,
            `store_detail_${storeCode}`,
            1420,
            720
        );
    },

    /**
     * 파트너사 상세 팝업
     */
    openPartnerDetail: function (partnerCode) {
        this.openFixedPopup(
            `/partners/popup/${partnerCode}`,
            `partner_detail_${partnerCode}`,
            1420,
            720
        );
    },
    /** 담당자 상세 팝업
     */
    openUserDetail: function (userCode) {
        this.openFixedPopup(
            `/users/popup/${userCode}`,
            `users_detail_${userCode}`,
            900,
            720
        );
    },

    /** 관리자 계정 상세 팝업 */
    openAdminAccountDetail: function (userCode) {
        this.openFixedPopup(
            `/admin/accounts/popup/${userCode}`,
            `admin_account_detail_${userCode}`,
            900,
            800
        );
    },

    /** 관리자 계정 신규 등록 팝업 */
    openAdminAccountCreate: function () {
        this.openFixedPopup(
            `/admin/accounts/new`,
            `admin_account_create`,
            900,
            800
        );
    },

    /** 비밀번호 초기화 공통 팝업 */
    openPasswordReset: function (targetType, userCode) {
        this.openFixedPopup(
            `/password-reset/popup/${targetType}/${userCode}`,
            `password_reset_${targetType}_${userCode}`,
            520,
            550
        );
    },

    /**
     * 공통 팝업 열기
     *
     * 기존 브라우저 창의 중앙을 기준으로 팝업 위치를 계산한다.
     */
    
    openFixedPopup: function (url, name, width, height) {
        // 현재 브라우저 창의 좌측 상단 위치를 가져온다.
        // screenX / screenY는 대부분의 최신 브라우저에서 사용된다.
        // screenLeft / screenTop은 일부 브라우저 호환용이다.
        const browserLeft = window.screenX ?? window.screenLeft ?? 0;
        const browserTop = window.screenY ?? window.screenTop ?? 0;

        // 현재 브라우저 창의 실제 외곽 크기를 기준으로 중앙 좌표를 계산한다.
        // outerWidth / outerHeight를 우선 사용하고,
        // 값이 없으면 innerWidth / innerHeight를 사용한다.
        const browserWidth = window.outerWidth || window.innerWidth;
        const browserHeight = window.outerHeight || window.innerHeight;

        // 현재 브라우저 창 중앙에 팝업이 뜨도록 위치를 계산한다.
        const left = Math.max(browserLeft + (browserWidth - width) / 2, 0);
        const top = Math.max(browserTop + (browserHeight - height) / 2, 0);

        // window.open 옵션.
        // 일부 옵션은 브라우저 정책에 따라 무시될 수 있다.
        const features = [
            `width=${width}`,
            `height=${height}`,
            `left=${Math.round(left)}`,
            `top=${Math.round(top)}`,
            "resizable=no",
            "scrollbars=no",
            "menubar=no",
            "toolbar=no",
            "location=no",
            "status=no",
            "popup=yes"
        ].join(",");

        const popup = window.open(url, name, features);

        // 팝업이 정상적으로 열렸다면 포커스를 이동한다.
        // 브라우저 팝업 차단이 활성화되어 있으면 popup이 null일 수 있다.
        if (popup) {
            popup.focus();
        }
    }
    /**
    * 팝업 부모창 새로고침
    */
    refreshOpener: function () {
        if (window.opener && !window.opener.closed) {
            window.opener.location.reload();
        }
    }
};