// 任意の JSON を POST したいとき
async function postJson(url, payload) {
    const res = await fetch(url, {
        method: 'POST',
        headers: {
        'Content-Type': 'application/json',
        },
        body: JSON.stringify(payload),
    });

    if (!res.ok) {
        // 4xx / 5xx エラーの場合
        throw new Error(`HTTP ${res.status} – ${res.statusText}`);
    }
    return await res.json();
}

async function post(className, typeName) {
    postJson('http://%s', { class: className, type: typeName })
        .then(data => console.log('レスポンス:', data))
        .catch(err => console.error('失敗:', err));
}