let fieldData;

window.addEventListener('onWidgetLoad', function (obj) {
    console.log('Widget loaded');
    fieldData = obj.detail.fieldData;
    console.log('Field data:', fieldData);
    playClips();
});

async function fetchClips(startDate, endDate) {
    try {
        console.log('Fetching clips from', startDate, 'to', endDate);
        // Ensure dates are in ISO 8601 format
        const formattedStartDate = new Date(startDate).toISOString();
        const formattedEndDate = new Date(endDate).toISOString();

        const response = await fetch(`http://localhost:5252/clips?start_date=${formattedStartDate}&end_date=${formattedEndDate}`);
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`Failed to fetch clips: ${response.statusText} - ${errorText}`);
        }

        const clips = await response.json();
        console.log('Fetched clips:', clips);
        return clips;
    } catch (error) {
        updateDebugMessages(`Error fetching clips: ${error.message}`);
        return [];
    }
}

async function playClips() {
    try {
        let startDate = fieldData['startDate'];
        let endDate = fieldData['endDate'];

        // Validate and format dates
        if (!startDate || isNaN(new Date(startDate).getTime())) {
            throw new Error('Invalid start date');
        }
        if (!endDate || isNaN(new Date(endDate).getTime())) {
            throw new Error('Invalid end date');
        }

        const clips = await fetchClips(startDate, endDate);
        const clipPlayer = document.getElementById('clip-player');

        if (!clipPlayer) {
            updateDebugMessages('No element with ID "clip-player" found');
            return;
        }

        if (clips.length === 0) {
            clipPlayer.innerText = 'No clips available';
            return;
        }

        let currentClipIndex = 0;

        function playNextClip() {
            if (currentClipIndex >= clips.length) {
                currentClipIndex = 0;
            }

            const clip = clips[currentClipIndex];
            updateDebugMessages(`Playing clip: ${clip.title}`);
            clipPlayer.src = clip.thumbnail_url.replace('-preview-480x272.jpg', '.mp4');
            clipPlayer.play();
            currentClipIndex++;
        }

        clipPlayer.addEventListener('ended', playNextClip);
        playNextClip();
    } catch (error) {
        updateDebugMessages(`Error: ${error.message}`);
    }
}

function updateDebugMessages(message) {
    console.log(message);
    const debugMessages = document.getElementById('debug-messages');
    debugMessages.innerText = message;
}