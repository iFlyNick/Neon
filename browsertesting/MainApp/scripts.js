$(function () {
    "use strict";
    
    if (!$) console.error('jQuery is not loaded');
    
    let commandTable = undefined;

    const tooltipTriggerList = $('[data-bs-toggle="tooltip"]');
    const tooltipList = [...tooltipTriggerList].map(tooltipTriggerE1 => new bootstrap.Tooltip(tooltipTriggerE1));
    
    function buttonBinding() {
        $('.sidebar-section button').on('click', function () {
            navbarSectionToggle($(this));
        });

        $('.sidebar-link').on('click', function () {
            toggleSidebarActive($(this));
        });
        
        $('#copy-overlay-url').on('click', function () {
            copyOverlayUrl($(this)); 
        });
        
        $('#chat-delay-input').on('input', function () {
            syncChatDelayOutput($(this));
        });
        
        $('#chat-message-remove-delay-input').on('input', function () {
            syncChatMessageRemoveDelayOutput($(this));
        });
        
        $('#chat-font-size-input').on('input', function () {
            syncChatFontSizeOutput($(this));
        });

        $('#chat-message-remove-delay-disable').on('change', function () {
            toggleRemoveDelayInput($(this));
        });

        syncChatDelayOutput($('#chat-delay-input'));
        syncChatMessageRemoveDelayOutput($('#chat-message-remove-delay-input'));
        syncChatFontSizeOutput($('#chat-font-size-input'));
        toggleRemoveDelayInput($('#chat-message-remove-delay-disable'));
    }

    function toggleRemoveDelayInput(element) {
        element.is(':checked') ? $('#chat-message-remove-delay-input').prop('disabled', true) : $('#chat-message-remove-delay-input').prop('disabled', false);
    }
    
    function syncChatMessageRemoveDelayOutput(element) {
        $('#chat-message-remove-delay-value').text(`${element.val()}s`);
    }
    
    function syncChatDelayOutput(element) {
        $('#chat-delay-value').text(`${element.val()}s`);
    }
    
    function syncChatFontSizeOutput(element) {
        $('#chat-font-size-value').text(`${element.val()}px`);
    }

    function copyOverlayUrl(element) {
        var textToCopy = element.attr('value');
        navigator.clipboard.writeText(textToCopy).then(function() {
            var originalText = element.html();
            element.html('<i class="fas fa-check"></i> Copied!');
            setTimeout(function() {
                element.html(originalText);
            }, 5000);
        }, function(err) {
            console.error('Could not copy text: ', err);
        });
    }
    
    function activateStoredSidebarState() {
        var storedState = localStorage.getItem('activeSidebar');
        if (storedState) {
            var targetElement = $('#sidebar-' + storedState);
            if (targetElement.length) {
                toggleSidebarActive(targetElement);
            } else {
                toggleSidebarActive($('#sidebar-dashboard-home'));
            }
        } else {
            toggleSidebarActive($('#sidebar-dashboard-home'));
        }
    }
    
    function storeSidebarState(element) {
        var targetId = element.attr('id').replace('sidebar-', '');
        localStorage.setItem('activeSidebar', targetId);
    }
    
    function toggleSidebarActive(element) {
        if (element.hasClass('selected')) 
            return;
        
        element.addClass('selected');
        $('#sidebar-main .sidebar-link').not(element).removeClass('selected');

        var targetId = element.attr('id').replace('sidebar-', '');
        $('#main-content #content').children().addClass('d-none');
        $('#main-content #content #' + targetId).removeClass('d-none');
        
        switch (targetId) {
            case 'dashboard-home':
                initDashboardHome();
                break;
            case 'alerts':
                initAlerts();
                break;
            case 'chatbot-commands':
                initCommands();
                break;
            case 'chatbot-setup':
                initSetup();
                break;
            default:
                break;
        }

        storeSidebarState(element);
    }
    
    function initDashboardHome() {
        
    }
    
    function initAlerts() {
        
    }
    
    function initCommands() {
        if (commandTable) {
            commandTable.destroy();
        }

        commandTable = new DataTable('#command-table', {
            paging: false,
            searching: true,
            info: false
        });
    }
    
    function initSetup() {
        
    }

    function navbarSectionToggle(element) {
        if (element.hasClass('expanded')) {
            element.removeClass('expanded').addClass('collapsed');
            element.find('i').css('transform', 'rotate(-90deg)');
            element.siblings('div.sidebar-subsection').slideUp(300);
        } else {
            element.removeClass('collapsed').addClass('expanded');
            element.find('i').css('transform', 'rotate(0deg)');
            element.siblings('div.sidebar-subsection').slideDown(300);
        }
    }

    buttonBinding();
    activateStoredSidebarState();
});