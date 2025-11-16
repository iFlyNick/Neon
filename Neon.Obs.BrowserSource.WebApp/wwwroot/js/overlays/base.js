window.Overlay.onInit = () => {
    console.log('Overlay type "base" initialized');
};

window.Overlay.onMessage = (msg) => {
    handleMessage(msg);
};

function handleMessage(msg) {
    console.log(msg);
}