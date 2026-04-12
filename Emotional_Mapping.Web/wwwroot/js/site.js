// Global emotion name mapping by locale
var geoFeelEmotionLabels = {
    bg: {
        'Calm': 'Спокойствие',
        'Joy': 'Радост',
        'Nostalgia': 'Носталгия',
        'Tension': 'Напрежение',
        'Inspiration': 'Вдъхновение',
        'Energetic': 'Енергично',
        'Romantic': 'Романтика',
        'Social': 'Социално',
        'Happy': 'Щастие',
        'Sad': 'Тъга',
        'Anxiety': 'Тревожност',
        'Relaxed': 'Релакс',
        'Angry': 'Гняв',
        'Bored': 'Скука',
        'Safe': 'Сигурност',
        'Unsafe': 'Опасност',
        'Excited': 'Еуфория',
        'Lonely': 'Самота'
    },
    en: {
        'Calm': 'Calm',
        'Joy': 'Joy',
        'Nostalgia': 'Nostalgia',
        'Tension': 'Tension',
        'Inspiration': 'Inspiration',
        'Energetic': 'Energetic',
        'Romantic': 'Romantic',
        'Social': 'Social',
        'Happy': 'Happy',
        'Sad': 'Sad',
        'Anxiety': 'Anxiety',
        'Relaxed': 'Relaxed',
        'Angry': 'Angry',
        'Bored': 'Bored',
        'Safe': 'Safe',
        'Unsafe': 'Unsafe',
        'Excited': 'Excited',
        'Lonely': 'Lonely'
    }
};

var geoFeelEmotionNamesById = {
    1: 'Calm',
    2: 'Joy',
    3: 'Nostalgia',
    4: 'Tension',
    5: 'Inspiration',
    6: 'Energetic',
    7: 'Romantic',
    8: 'Social',
    9: 'Happy',
    10: 'Sad',
    11: 'Anxiety',
    12: 'Relaxed',
    13: 'Angry',
    14: 'Bored',
    15: 'Safe',
    16: 'Unsafe',
    17: 'Excited',
    18: 'Lonely'
};

function geoFeelCurrentLanguage() {
    var lang = document && document.documentElement ? document.documentElement.lang : 'bg';
    return String(lang || 'bg').toLowerCase().indexOf('en') === 0 ? 'en' : 'bg';
}

function decodeHtmlEntities(value) {
    if (value === null || value === undefined) return value;

    var text = String(value);
    if (text.indexOf('&') === -1) return text;

    var textarea = document.createElement('textarea');
    textarea.innerHTML = text;
    return textarea.value;
}

function normalizeEmotionKey(value) {
    if (value === null || value === undefined) return null;

    var decoded = decodeHtmlEntities(value);
    var text = String(decoded).trim();
    if (!text) return null;

    if (/^\d+$/.test(text)) {
        return geoFeelEmotionNamesById[Number(text)] || null;
    }

    if (geoFeelEmotionLabels.en[text]) return text;

    var key = text.charAt(0).toUpperCase() + text.slice(1).toLowerCase();
    return geoFeelEmotionLabels.en[key] ? key : text;
}

function toLocalizedEmotion(en) {
    var key = normalizeEmotionKey(en);
    if (!key) return '—';

    var map = geoFeelEmotionLabels[geoFeelCurrentLanguage()] || geoFeelEmotionLabels.bg;
    return map[key] || key;
}

function toBgEmotion(en) {
    return toLocalizedEmotion(en);
}

function toLocalizedTimeLabel(label) {
    if (!label) return '';

    var normalized = String(label).trim().toLowerCase();
    var isEnglish = geoFeelCurrentLanguage() === 'en';

    if (normalized === 'сутрин' || normalized === 'morning') return isEnglish ? 'Morning' : 'Сутрин';
    if (normalized === 'следобед' || normalized === 'afternoon') return isEnglish ? 'Afternoon' : 'Следобед';
    if (normalized === 'вечер' || normalized === 'evening') return isEnglish ? 'Evening' : 'Вечер';

    return label;
}

// Keep navigation labels in a custom hover pill instead of the native title tooltip
document.addEventListener('DOMContentLoaded', function () {
    document.querySelectorAll('.em-nav-ico').forEach(function (el) {
        var label = el.dataset.tip || el.getAttribute('aria-label') || el.getAttribute('title');
        if (label) {
            el.dataset.tip = label;
        }

        if (el.hasAttribute('title')) {
            el.removeAttribute('title');
        }
    });
});

window.GeoFeelLocation = (function () {
    var defaultGeoOptions = {
        enableHighAccuracy: true,
        timeout: 12000,
        maximumAge: 0
    };

    var defaultExactAccuracyMeters = 1200;
    var defaultUsableAccuracyMeters = 10000;

    function formatAccuracy(accuracyMeters) {
        if (!Number.isFinite(accuracyMeters) || accuracyMeters <= 0) return '';
        if (geoFeelCurrentLanguage() === 'en') {
            if (accuracyMeters < 1000) return 'about ' + Math.round(accuracyMeters) + ' m';
            if (accuracyMeters < 10000) return 'about ' + (accuracyMeters / 1000).toFixed(1) + ' km';
            return 'about ' + Math.round(accuracyMeters / 1000) + ' km';
        }
        if (accuracyMeters < 1000) return 'около ' + Math.round(accuracyMeters) + ' м';
        if (accuracyMeters < 10000) return 'около ' + (accuracyMeters / 1000).toFixed(1) + ' км';
        return 'около ' + Math.round(accuracyMeters / 1000) + ' км';
    }

    async function reverseGeocode(lat, lng) {
        try {
            var acceptLanguage = geoFeelCurrentLanguage();
            var response = await fetch(
                'https://nominatim.openstreetmap.org/reverse?lat=' + lat + '&lon=' + lng + '&format=json&accept-language=' + acceptLanguage,
                { headers: { 'Accept-Language': acceptLanguage } }
            );
            var data = await response.json();
            var address = data && data.address ? data.address : null;
            var parts = [
                decodeHtmlEntities(address && (address.road || address.pedestrian || address.footway)),
                decodeHtmlEntities(address && (address.suburb || address.neighbourhood || address.quarter)),
                decodeHtmlEntities(address && (address.city || address.town || address.village || address.municipality))
            ].filter(Boolean);

            return parts.length
                ? parts.join(', ')
                : (data && data.display_name ? decodeHtmlEntities(data.display_name).split(',').slice(0, 3).join(', ') : null);
        } catch (error) {
            return null;
        }
    }

    function haversineKm(lat1, lng1, lat2, lng2) {
        var toRad = function (degrees) { return degrees * Math.PI / 180; };
        var earthRadiusKm = 6371;
        var dLat = toRad(lat2 - lat1);
        var dLng = toRad(lng2 - lng1);
        var a = Math.sin(dLat / 2) * Math.sin(dLat / 2) +
            Math.cos(toRad(lat1)) * Math.cos(toRad(lat2)) *
            Math.sin(dLng / 2) * Math.sin(dLng / 2);

        return earthRadiusKm * 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    }

    function setSelectedCity(selectEl, cityId, options) {
        options = options || {};

        if (!selectEl || !cityId) return false;
        if (selectEl.value === cityId && !options.forceEvent) return false;

        selectEl.dataset.skipCenter = options.skipCenter ? 'true' : '';
        selectEl.dataset.skipStatus = options.skipStatus ? 'true' : '';
        selectEl.value = cityId;
        selectEl.dispatchEvent(new Event('change'));
        return true;
    }

    function findNearestCity(cityCatalog, lat, lng) {
        if (!Array.isArray(cityCatalog) || !cityCatalog.length) return null;

        var nearest = null;
        cityCatalog.forEach(function (city) {
            var cityLat = city && city.center ? Number(city.center.lat) : NaN;
            var cityLng = city && city.center ? Number(city.center.lng) : NaN;
            if (!Number.isFinite(cityLat) || !Number.isFinite(cityLng)) return;

            var distanceKm = haversineKm(lat, lng, cityLat, cityLng);
            if (!nearest || distanceKm < nearest.distanceKm) {
                nearest = { city: city, distanceKm: distanceKm };
            }
        });

        return nearest;
    }

    function syncNearestCity(selectEl, cityCatalog, lat, lng, accuracyMeters, options) {
        options = options || {};

        var nearest = findNearestCity(cityCatalog, lat, lng);
        if (!nearest) return null;

        var maxDistanceKm = typeof options.maxDistanceKm === 'number'
            ? options.maxDistanceKm
            : (accuracyMeters <= defaultExactAccuracyMeters ? 45 : 20);

        if (nearest.distanceKm > maxDistanceKm) return null;

        if (selectEl && selectEl.value && selectEl.value !== nearest.city.id && !options.force) {
            return nearest;
        }

        if (selectEl && selectEl.value !== nearest.city.id) {
            setSelectedCity(selectEl, nearest.city.id, {
                skipCenter: options.skipCenter !== false,
                skipStatus: options.skipStatus !== false,
                forceEvent: options.forceEvent === true
            });
        }

        return nearest;
    }

    function getCurrentPosition(geoOptions) {
        return new Promise(function (resolve, reject) {
            if (!navigator.geolocation) {
                reject({ code: 'unsupported' });
                return;
            }

            if (!isGeolocationSecureContext()) {
                reject({ code: 'insecure' });
                return;
            }

            navigator.geolocation.getCurrentPosition(resolve, reject, geoOptions || defaultGeoOptions);
        });
    }

    function isGeolocationSecureContext() {
        if (window.isSecureContext) return true;

        var hostname = window.location && window.location.hostname
            ? window.location.hostname
            : '';

        return hostname === 'localhost' || hostname === '127.0.0.1' || hostname === '[::1]';
    }

    async function getLocationPermissionState() {
        if (!navigator.geolocation) return 'unsupported';
        if (!isGeolocationSecureContext()) return 'insecure';
        if (!navigator.permissions || typeof navigator.permissions.query !== 'function') return 'unknown';

        try {
            var permission = await navigator.permissions.query({ name: 'geolocation' });
            return permission && permission.state ? permission.state : 'unknown';
        } catch (error) {
            return 'unknown';
        }
    }

    async function fetchIpLocation() {
        try {
            var response = await fetch('https://ipapi.co/json/', {
                headers: { 'Accept': 'application/json' }
            });

            if (!response.ok) return null;

            var data = await response.json();
            var lat = Number(data.latitude);
            var lng = Number(data.longitude);

            if (!Number.isFinite(lat) || !Number.isFinite(lng)) return null;

            var locationName = await reverseGeocode(lat, lng);
            var fallbackName = [data.city, data.region, data.country_name]
                .map(decodeHtmlEntities)
                .filter(Boolean)
                .join(', ');

            return {
                ok: true,
                source: 'ip',
                lat: lat,
                lng: lng,
                accuracyMeters: null,
                isExact: false,
                isUsable: true,
                isApproximate: true,
                locationName: locationName || fallbackName || null,
                label: (locationName || fallbackName || (geoFeelCurrentLanguage() === 'en' ? 'Approximate location' : 'Приблизителна локация'))
                    + (geoFeelCurrentLanguage() === 'en' ? ' (approximate network location)' : ' (приблизително по мрежа)')
            };
        } catch (error) {
            return null;
        }
    }

    async function tryIpFallback(allowIpFallback, reason, extra) {
        if (!allowIpFallback) {
            return {
                ok: false,
                source: 'gps',
                reason: reason,
                accuracyMeters: extra && Number.isFinite(extra.accuracyMeters) ? extra.accuracyMeters : null
            };
        }

        var ipLocation = await fetchIpLocation();
        if (!ipLocation) {
            return {
                ok: false,
                source: 'gps',
                reason: reason,
                accuracyMeters: extra && Number.isFinite(extra.accuracyMeters) ? extra.accuracyMeters : null
            };
        }

        if (extra && Number.isFinite(extra.accuracyMeters)) {
            ipLocation.gpsAccuracyMeters = extra.accuracyMeters;
        }

        ipLocation.fallbackReason = reason;
        return ipLocation;
    }

    async function getInitialLocation(options) {
        options = options || {};

        var permissionState = await getLocationPermissionState();
        var allowIpFallback = options.allowIpFallback !== false;

        if (permissionState === 'granted') {
            var grantedLocation = await getBestEffortLocation(options);
            grantedLocation.permissionState = permissionState;
            return grantedLocation;
        }

        var initialLocation = await tryIpFallback(
            allowIpFallback,
            permissionState === 'denied' ? 'denied' :
                permissionState === 'insecure' ? 'insecure' :
                permissionState === 'unsupported' ? 'unsupported' :
                permissionState === 'prompt' ? 'awaiting-consent' :
                'permission-unknown'
        );

        initialLocation.permissionState = permissionState;
        initialLocation.needsUserActionForExact = permissionState === 'prompt' || permissionState === 'unknown';
        return initialLocation;
    }

    async function getBestEffortLocation(options) {
        options = options || {};

        var allowIpFallback = options.allowIpFallback !== false;
        var exactAccuracyMeters = Number(options.exactAccuracyMeters || defaultExactAccuracyMeters);
        var usableAccuracyMeters = Number(options.usableAccuracyMeters || defaultUsableAccuracyMeters);
        var geoOptions = options.geolocationOptions || defaultGeoOptions;

        if (!navigator.geolocation) {
            return tryIpFallback(allowIpFallback, 'unsupported');
        }

        try {
            var position = await getCurrentPosition(geoOptions);
            var lat = Number(position.coords.latitude);
            var lng = Number(position.coords.longitude);
            var accuracyMeters = Number(position.coords.accuracy || 0);
            var isExact = accuracyMeters > 0 && accuracyMeters <= exactAccuracyMeters;
            var isUsable = accuracyMeters > 0 && accuracyMeters <= usableAccuracyMeters;

            if (!Number.isFinite(lat) || !Number.isFinite(lng)) {
                return tryIpFallback(allowIpFallback, 'invalid');
            }

            if (!isUsable) {
                return tryIpFallback(allowIpFallback, 'inaccurate', { accuracyMeters: accuracyMeters });
            }

            var locationName = await reverseGeocode(lat, lng);

            return {
                ok: true,
                source: 'gps',
                lat: lat,
                lng: lng,
                accuracyMeters: accuracyMeters,
                isExact: isExact,
                isUsable: true,
                isApproximate: !isExact,
                locationName: locationName,
                label: (locationName || (geoFeelCurrentLanguage() === 'en' ? 'Current location' : 'Текуща локация'))
                    + (accuracyMeters ? ' (' + formatAccuracy(accuracyMeters) + ')' : '')
            };
        } catch (error) {
            var reason = 'error';
            if (error && typeof error.code === 'number') {
                if (error.code === 1) reason = 'denied';
                if (error.code === 2) reason = 'unavailable';
                if (error.code === 3) reason = 'timeout';
            } else if (error && error.code === 'insecure') {
                reason = 'insecure';
            } else if (error && error.code === 'unsupported') {
                reason = 'unsupported';
            }
            return tryIpFallback(allowIpFallback, reason);
        }
    }

    return {
        defaults: {
            geolocationOptions: defaultGeoOptions,
            exactAccuracyMeters: defaultExactAccuracyMeters,
            usableAccuracyMeters: defaultUsableAccuracyMeters
        },
        formatAccuracy: formatAccuracy,
        reverseGeocode: reverseGeocode,
        haversineKm: haversineKm,
        setSelectedCity: setSelectedCity,
        findNearestCity: findNearestCity,
        syncNearestCity: syncNearestCity,
        getLocationPermissionState: getLocationPermissionState,
        getInitialLocation: getInitialLocation,
        getBestEffortLocation: getBestEffortLocation,
        fetchIpLocation: fetchIpLocation
    };
})();

window.GeoFeelI18n = {
    currentLanguage: geoFeelCurrentLanguage,
    emotionKey: normalizeEmotionKey,
    emotion: toLocalizedEmotion,
    timeLabel: toLocalizedTimeLabel
};
