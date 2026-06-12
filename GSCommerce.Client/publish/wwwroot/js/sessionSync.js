// Session Synchronization across multiple browser tabs/windows
window.sessionSync = {
    startListening: function (dotnetHelper) {
        console.log('🔄 Session sync listener started');
        
        window.addEventListener('storage', function (e) {
            // Only trigger when authToken is removed (logout)
            if (e.key === 'authToken' && e.oldValue && !e.newValue) {
                console.log('🚪 Logout detected in another window');
                dotnetHelper.invokeMethodAsync('OnSessionEndedInAnotherWindow');
            }
        });
    }
};



