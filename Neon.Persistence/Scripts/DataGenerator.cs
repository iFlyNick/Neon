using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Neon.Persistence.EntityModels.Twitch;
using Neon.Persistence.NeonContext;

namespace Neon.Persistence.Scripts;

public class DataGenerator(ILogger<DataGenerator> logger, NeonDbContext neonContext) : IDataGenerator
{
    public async Task PreloadDbData(CancellationToken ct = default)
    {
        //TODO: preload app account for true startup for given user?
        await PreloadSubscriptionTypes(ct);
        await PreloadAuthorizationScopes(ct);
        
        await PreloadAuthorizationScopeSubscriptionTypes(ct);
    }
    
    private async Task PreloadSubscriptionTypes(CancellationToken ct = default)
    {
        var subscriptionTypes = GetSubscriptionTypes();

        var dbTypes = await neonContext.SubscriptionType.ToListAsync(ct);

        if (dbTypes.Count == 0)
        {
            //db has nothing, just add the range
            logger.LogDebug("No subscription types found in db, adding default types.");
            neonContext.SubscriptionType.AddRange(subscriptionTypes);
            await neonContext.SaveChangesAsync(ct);
            return;
        }
        
        //db has some data, check if we need to add any new types
        foreach (var type in subscriptionTypes)
        {
            var dbType = dbTypes.FirstOrDefault(s => s.Name == type.Name && s.Version == type.Version);
            if (dbType is null)
            {
                //this type doesn't exist in the db, add it
                neonContext.SubscriptionType.Add(type);
                continue;
            }
            
            //type exists, ensure it's updated
            dbType.Description = type.Description;

            if (neonContext.Entry(dbType).State == EntityState.Unchanged)
                continue;

            neonContext.SubscriptionType.Update(dbType);
        }

        //purge any types that are no longer in the list
        var removeList = dbTypes.Where(s => !subscriptionTypes.Any(t => t.Name == s.Name && t.Version == s.Version)).ToList();
        if (removeList.Count > 0)
        {
            neonContext.SubscriptionType.RemoveRange(removeList);
            logger.LogDebug("Removed {count} subscription types from db.", removeList.Count);
        }
        
        if (neonContext.ChangeTracker.HasChanges())
            await neonContext.SaveChangesAsync(ct);
    }

    private static List<SubscriptionType> GetSubscriptionTypes()
    {
        //define base subscription types to ensure the data exists in the db on startup
        //https://dev.twitch.tv/docs/eventsub/eventsub-subscription-types/
        var subscriptionTypes = new List<SubscriptionType>
        {
            new()
            {
                //there is also a version 1 of this event subscription, but excluding it for now. this isn't used anyways
                Name = "automod.message.hold",
                Version = "2",
                Description = "A user is notified if a message is caught by automod for review. Only public blocked terms trigger notifications, not private ones."
            },
            new()
            {
                //there is also a version 1 of this event subscription, but excluding it for now. this isn't used anyways
                Name = "automod.message.update",
                Version = "2",
                Description = "A message in the automod queue had its status changed. Only public blocked terms trigger notifications, not private ones."
            },
            new()
            {
                Name = "automod.settings.update",
                Version = "1",
                Description = "A notification is sent when a broadcaster’s automod settings are updated."
            },
            new()
            {
                Name = "automod.terms.update",
                Version = "1",
                Description = "A notification is sent when a broadcaster’s automod terms are updated. Changes to private terms are not sent."
            },
            new()
            {
                Name = "channel.bits.use",
                Version = "1",
                Description = "A notification is sent whenever Bits are used on a channel."
            },
            new()
            {
                Name = "channel.update",
                Version = "2",
                Description = "A broadcaster updates their channel properties e.g., category, title, content classification labels, broadcast, or language."
            },
            new()
            {
                Name = "channel.follow",
                Version = "2",
                Description = "A specified channel receives a follow."
            },
            new()
            {
                Name = "channel.ad_break.begin",
                Version = "1",
                Description = "A midroll commercial break has started running."
            },
            new()
            {
                Name = "channel.chat.clear",
                Version = "1",
                Description = "A moderator or bot has cleared all messages from the chat room."
            },
            new()
            {
                Name = "channel.chat.clear_user_messages",
                Version = "1",
                Description = "A moderator or bot has cleared all messages from a specific user."
            },
            new()
            {
                Name = "channel.chat.message",
                Version = "1",
                Description = "Any user sends a message to a specific chat room."
            },
            new()
            {
                Name = "channel.chat.message_delete",
                Version = "1",
                Description = "A moderator has removed a specific message."
            },
            new()
            {
                Name = "channel.chat.notification",
                Version = "1",
                Description = "A notification for when an event that appears in chat has occurred."
            },
            new()
            {
                Name = "channel.chat_settings.update",
                Version = "1",
                Description = "A notification for when a broadcaster’s chat settings are updated."
            },
            new()
            {
                Name = "channel.chat.user_message_hold",
                Version = "1",
                Description = "A user is notified if their message is caught by automod."
            },
            new()
            {
                Name = "channel.chat.user_message_update",
                Version = "1",
                Description = "A user is notified if their message’s automod status is updated."
            },
            new()
            {
                Name = "channel.shared_chat.begin",
                Version = "1",
                Description = "A notification when a channel becomes active in an active shared chat session."
            },
            new()
            {
                Name = "channel.shared_chat.update",
                Version = "1",
                Description = "A notification when the active shared chat session the channel is in changes."
            },
            new()
            {
                Name = "channel.shared_chat.end",
                Version = "1",
                Description = "A notification when a channel leaves a shared chat session or the session ends."
            },
            new()
            {
                Name = "channel.subscribe",
                Version = "1",
                Description = "A notification is sent when a specified channel receives a subscriber. This does not include resubscribes."
            },
            new()
            {
                Name = "channel.subscription.end",
                Version = "1",
                Description = "A notification when a subscription to the specified channel ends."
            },
            new()
            {
                Name = "channel.subscription.gift",
                Version = "1",
                Description = "A notification when a viewer gives a gift subscription to one or more users in the specified channel."
            },
            new()
            {
                Name = "channel.subscription.message",
                Version = "1",
                Description = "A notification when a user sends a resubscription chat message in a specific channel."
            },
            new()
            {
                Name = "channel.cheer",
                Version = "1",
                Description = "A user cheers on the specified channel."
            },
            new()
            {
                Name = "channel.raid",
                Version = "1",
                Description = "A broadcaster raids another broadcaster’s channel."
            },
            new()
            {
                Name = "channel.ban",
                Version = "1",
                Description = "A viewer is banned from the specified channel."
            },
            new()
            {
                Name = "channel.unban",
                Version = "1",
                Description = "A viewer is unbanned from the specified channel."
            },
            new()
            {
                Name = "channel.unban_request.create",
                Version = "1",
                Description = "A user creates an unban request."
            },
            new()
            {
                Name = "channel.unban_request.resolve",
                Version = "1",
                Description = "An unban request has been resolved."
            },
            new()
            {
                //there is also a version 1 of this event subscription, but excluding it for now. this isn't used anyways
                Name = "channel.moderate",
                Version = "2",
                Description = "A moderator performs a moderation action in a channel. Includes warnings."
            },
            new()
            {
                Name = "channel.moderator.add",
                Version = "1",
                Description = "Moderator privileges were added to a user on a specified channel."
            },
            new()
            {
                Name = "channel.moderator.remove",
                Version = "1",
                Description = "Moderator privileges were removed from a user on a specified channel."
            },
            new()
            {
                Name = "channel.guest_star_session.begin",
                Version = "beta",
                Description = "The host began a new Guest Star session."
            },
            new()
            {
                Name = "channel.guest_star_session.end",
                Version = "beta",
                Description = "A running Guest Star session has ended."
            },
            new()
            {
                Name = "channel.guest_star_guest.update",
                Version = "beta",
                Description = "A guest or a slot is updated in an active Guest Star session."
            },
            new()
            {
                Name = "channel.guest_star_settings.update",
                Version = "beta",
                Description = "The host preferences for Guest Star have been updated."
            },
            new()
            {
                //there is also a version 1 of this event subscription, but excluding it for now. this isn't used anyways
                Name = "channel.channel_points_automatic_reward_redemption.add",
                Version = "2",
                Description = "A viewer has redeemed an automatic channel points reward on the specified channel."
            },
            new()
            {
                Name = "channel.channel_points_custom_reward.add",
                Version = "1",
                Description = "A custom channel points reward has been created for the specified channel."
            },
            new()
            {
                Name = "channel.channel_points_custom_reward.update",
                Version = "1",
                Description = "A custom channel points reward has been updated for the specified channel."
            },
            new()
            {
                Name = "channel.channel_points_custom_reward.remove",
                Version = "1",
                Description = "A custom channel points reward has been removed from the specified channel."
            },
            new()
            {
                Name = "channel.channel_points_custom_reward_redemption.add",
                Version = "1",
                Description = "A viewer has redeemed a custom channel points reward on the specified channel."
            },
            new()
            {
                Name = "channel.channel_points_custom_reward_redemption.update",
                Version = "1",
                Description = "A redemption of a channel points custom reward has been updated for the specified channel."
            },
            new()
            {
                Name = "channel.poll.begin",
                Version = "1",
                Description = "A poll started on a specified channel."
            },
            new()
            {
                Name = "channel.poll.progress",
                Version = "1",
                Description = "Users respond to a poll on a specified channel."
            },
            new()
            {
                Name = "channel.poll.end",
                Version = "1",
                Description = "A poll ended on a specified channel."
            },
            new()
            {
                Name = "channel.prediction.begin",
                Version = "1",
                Description = "A Prediction started on a specified channel."
            },
            new()
            {
                Name = "channel.prediction.progress",
                Version = "1",
                Description = "Users participated in a Prediction on a specified channel."
            },
            new()
            {
                Name = "channel.prediction.lock",
                Version = "1",
                Description = "A Prediction was locked on a specified channel."
            },
            new()
            {
                Name = "channel.prediction.end",
                Version = "1",
                Description = "A Prediction ended on a specified channel."
            },
            new()
            {
                Name = "channel.suspicious_user.message",
                Version = "1",
                Description = "A chat message has been sent by a suspicious user."
            },
            new()
            {
                Name = "channel.suspicious_user.update",
                Version = "1",
                Description = "A suspicious user has been updated."
            },
            new()
            {
                Name = "channel.vip.add",
                Version = "1",
                Description = "A VIP is added to the channel."
            },
            new()
            {
                Name = "channel.vip.remove",
                Version = "1",
                Description = "A VIP is removed from the channel."
            },
            new()
            {
                Name = "channel.warning.acknowledge",
                Version = "1",
                Description = "A user acknowledges a warning. Broadcasters and moderators can see the warning’s details."
            },
            new()
            {
                Name = "channel.warning.send",
                Version = "1",
                Description = "A user is sent a warning. Broadcasters and moderators can see the warning’s details."
            },
            new()
            {
                Name = "channel.charity_campaign.donate",
                Version = "1",
                Description = "Sends an event notification when a user donates to the broadcaster’s charity campaign."
            },
            new()
            {
                Name = "channel.charity_campaign.start",
                Version = "1",
                Description = "Sends an event notification when the broadcaster starts a charity campaign."
            },
            new()
            {
                Name = "channel.charity_campaign.progress",
                Version = "1",
                Description = "Sends an event notification when progress is made towards the campaign’s goal or when the broadcaster changes the fundraising goal."
            },
            new()
            {
                Name = "channel.charity_campaign.stop",
                Version = "1",
                Description = "Sends an event notification when the broadcaster stops a charity campaign."
            },
            new()
            {
                Name = "conduit.shard.disabled",
                Version = "1",
                Description = "Sends a notification when EventSub disables a shard due to the status of the underlying transport changing."
            },
            new()
            {
                Name = "drop.entitlement.grant",
                Version = "1",
                Description = "An entitlement for a Drop is granted to a user."
            },
            new()
            {
                Name = "extension.bits_transaction.create",
                Version = "1",
                Description = "A Bits transaction occurred for a specified Twitch Extension."
            },
            new()
            {
                Name = "channel.goal.begin",
                Version = "1",
                Description = "Get notified when a broadcaster begins a goal."
            },
            new()
            {
                Name = "channel.goal.progress",
                Version = "1",
                Description = "Get notified when progress (either positive or negative) is made towards a broadcaster’s goal."
            },
            new()
            {
                Name = "channel.goal.end",
                Version = "1",
                Description = "Get notified when a broadcaster ends a goal."
            },
            new()
            {
                Name = "channel.hype_train.begin",
                Version = "1",
                Description = "A Hype Train begins on the specified channel."
            },
            new()
            {
                Name = "channel.hype_train.progress",
                Version = "1",
                Description = "A Hype Train makes progress on the specified channel."
            },
            new()
            {
                Name = "channel.hype_train.end",
                Version = "1",
                Description = "A Hype Train ends on the specified channel."
            },
            new()
            {
                Name = "channel.shield_mode.begin",
                Version = "1",
                Description = "Sends a notification when the broadcaster activates Shield Mode."
            },
            new()
            {
                Name = "channel.shield_mode.end",
                Version = "1",
                Description = "Sends a notification when the broadcaster deactivates Shield Mode."
            },
            new()
            {
                Name = "channel.shoutout.create",
                Version = "1",
                Description = "Sends a notification when the specified broadcaster sends a Shoutout."
            },
            new()
            {
                Name = "channel.shoutout.receive",
                Version = "1",
                Description = "Sends a notification when the specified broadcaster receives a Shoutout."
            },
            new()
            {
                Name = "stream.online",
                Version = "1",
                Description = "The specified broadcaster starts a stream."
            },
            new()
            {
                Name = "stream.offline",
                Version = "1",
                Description = "The specified broadcaster stops a stream."
            },
            new()
            {
                Name = "user.authorization.grant",
                Version = "1",
                Description = "A user’s authorization has been granted to your client id."
            },
            new()
            {
                Name = "user.authorization.revoke",
                Version = "1",
                Description = "A user’s authorization has been revoked for your client id."
            },
            new()
            {
                Name = "user.update",
                Version = "1",
                Description = "A user has updated their account."
            },
            new()
            {
                Name = "user.whisper.message",
                Version = "1",
                Description = "A user receives a whisper."
            }
        };
        
        return subscriptionTypes;
    }

    private async Task PreloadAuthorizationScopes(CancellationToken ct = default)
    {
        var authScopes = GetAuthorizationScopes();

        var dbScopes = await neonContext.AuthorizationScope.ToListAsync(ct);

        if (dbScopes.Count == 0)
        {
            //db has nothing, just add the range
            logger.LogDebug("No auth scopes found in db, adding default types.");
            neonContext.AuthorizationScope.AddRange(authScopes);
            await neonContext.SaveChangesAsync(ct);
            return;
        }
        
        //db has some data, check if we need to add any new scopes
        foreach (var scope in authScopes)
        {
            var dbScope = dbScopes.FirstOrDefault(s => s.Name == scope.Name);
            if (dbScope is null)
                neonContext.AuthorizationScope.Add(scope);
        }

        //purge any types that are no longer in the list
        var removeList = dbScopes.Where(s => authScopes.All(t => t.Name != s.Name)).ToList();
        if (removeList.Count > 0)
        {
            neonContext.AuthorizationScope.RemoveRange(removeList);
            logger.LogDebug("Removed {count} auth scopes from db.", removeList.Count);
        }
        
        if (neonContext.ChangeTracker.HasChanges())
            await neonContext.SaveChangesAsync(ct);
    }

    private static List<AuthorizationScope> GetAuthorizationScopes()
    {
        //https://dev.twitch.tv/docs/authentication/scopes/#twitch-access-token-scopes
        var scopes = new List<AuthorizationScope>
        {
            new()
            {
                Name = "analytics:read:extensions"
            },
            new()
            {
                Name = "analytics:read:games"
            },
            new()
            {
                Name = "bits:read"
            },
            new()
            {
                Name = "channel:bot"
            },
            new()
            {
                Name = "channel:manage:ads"
            },
            new()
            {
                Name = "channel:read:ads"
            },
            new()
            {
                Name = "channel:manage:broadcast"
            },
            new()
            {
                Name = "channel:read:charity"
            },
            new()
            {
                Name = "channel:edit:commercial"
            },
            new()
            {
                Name = "channel:read:editors"
            },
            new()
            {
                Name = "channel:manage:extensions"
            },
            new()
            {
                Name = "channel:read:goals"
            },
            new()
            {
                Name = "channel:read:guest_star"
            },
            new()
            {
                Name = "channel:manage:guest_star"
            },
            new()
            {
                Name = "channel:read:hype_train"
            },
            new()
            {
                Name = "channel:manage:moderators"
            },
            new()
            {
                Name = "channel:read:polls"
            },
            new()
            {
                Name = "channel:manage:polls"
            },
            new()
            {
                Name = "channel:read:predictions"
            },
            new()
            {
                Name = "channel:manage:predictions"
            },
            new()
            {
                Name = "channel:manage:raids"
            },
            new()
            {
                Name = "channel:read:redemptions"
            },
            new()
            {
                Name = "channel:manage:redemptions"
            },
            new()
            {
                Name = "channel:manage:schedule"
            },
            new()
            {
                Name = "channel:read:stream_key"
            },
            new()
            {
                Name = "channel:read:subscriptions"
            },
            new()
            {
                Name = "channel:manage:videos"
            },
            new()
            {
                Name = "channel:read:vips"
            },
            new()
            {
                Name = "channel:manage:vips"
            },
            new()
            {
                Name = "channel:moderate"
            },
            new()
            {
                Name = "clips:edit"
            },
            new()
            {
                Name = "moderation:read"
            },
            new()
            {
                Name = "moderator:manage:announcements"
            },
            new()
            {
                Name = "moderator:manage:automod"
            },
            new()
            {
                Name = "moderator:read:automod_settings"
            },
            new()
            {
                Name = "moderator:manage:automod_settings"
            },
            new()
            {
                Name = "moderator:read:banned_users"
            },
            new()
            {
                Name = "moderator:manage:banned_users"
            },
            new()
            {
                Name = "moderator:read:blocked_terms"
            },
            new()
            {
                Name = "moderator:read:chat_messages"
            },
            new()
            {
                Name = "moderator:manage:blocked_terms"
            },
            new()
            {
                Name = "moderator:manage:chat_messages"
            },
            new()
            {
                Name = "moderator:read:chat_settings"
            },
            new()
            {
                Name = "moderator:manage:chat_settings"
            },
            new()
            {
                Name = "moderator:read:chatters"
            },
            new()
            {
                Name = "moderator:read:followers"
            },
            new()
            {
                Name = "moderator:read:guest_star"
            },
            new()
            {
                Name = "moderator:manage:guest_star"
            },
            new()
            {
                Name = "moderator:read:moderators"
            },
            new()
            {
                Name = "moderator:read:shield_mode"
            },
            new()
            {
                Name = "moderator:manage:shield_mode"
            },
            new()
            {
                Name = "moderator:read:shoutouts"
            },
            new()
            {
                Name = "moderator:manage:shoutouts"
            },
            new()
            {
                Name = "moderator:read:suspicious_users"
            },
            new()
            {
                Name = "moderator:read:unban_requests"
            },
            new()
            {
                Name = "moderator:manage:unban_requests"
            },
            new()
            {
                Name = "moderator:read:vips"
            },
            new()
            {
                Name = "moderator:read:warnings"
            },
            new()
            {
                Name = "moderator:manage:warnings"
            },
            new()
            {
                Name = "user:bot"
            },
            new()
            {
                Name = "user:edit"
            },
            new()
            {
                Name = "user:edit:broadcast"
            },
            new()
            {
                Name = "user:read:blocked_users"
            },
            new()
            {
                Name = "user:manage:blocked_users"
            },
            new()
            {
                Name = "user:read:broadcast"
            },
            new()
            {
                Name = "user:read:chat"
            },
            new()
            {
                Name = "user:manage:chat_color"
            },
            new()
            {
                Name = "user:read:email"
            },
            new()
            {
                Name = "user:read:emotes"
            },
            new()
            {
                Name = "user:read:follows"
            },
            new()
            {
                Name = "user:read:moderated_channels"
            },
            new()
            {
                Name = "user:read:subscriptions"
            },
            new()
            {
                Name = "user:read:whispers"
            },
            new()
            {
                Name = "user:manage:whispers"
            },
            new()
            {
                Name = "user:write:chat"
            }
        };
        
        return scopes;
    }

    private async Task PreloadAuthorizationScopeSubscriptionTypes(CancellationToken ct = default)
    {
        //need to fetch the types and scopes from the db to be able to create the relationship table as they join off ids only
        var dbTypes = await neonContext.SubscriptionType.ToListAsync(ct);
        var dbScopes = await neonContext.AuthorizationScope.ToListAsync(ct);
        
        if (dbTypes.Count == 0 || dbScopes.Count == 0)
        {
            logger.LogDebug("No subscription types or auth scopes found in db, unable to preload relationships.");
            return;
        }
        
        var authScopeSubscriptionTypes = new List<AuthorizationScopeSubscriptionType>();
        
        /*
         * example of the process:
         * scope:
         *  - bits:read
         * types:
         *  - channel.bits.use
         *  - channel.cheer
         */

        //not all scopes have types associated with them as not all scopes can be subscribed to via the eventsub.
        //this only maps the scopes that have types associated with them
        
        //TODO: import this as a json file so it's not compiled code?
        //key = auth scope, value = list of types
        var authScopeTypes = new Dictionary<string, List<string>>
        {
            {
                "analytics:read:extensions", 
                []
            },
            {
                "analytics:read:games", 
                []
            },
            { 
                "bits:read", 
                [ 
                    "channel.bits.use", 
                    "channel.cheer" 
                ] 
            },
            { 
                "channel:bot", 
                [ 
                    "channel.chat.clear", 
                    "channel.chat.clear_user_messages", 
                    "channel.chat.message", 
                    "channel.chat.message_delete", 
                    "channel.chat.notification", 
                    "channel.chat_settings.update"
                ] 
            },
            {
                "channel:manage:ads", 
                []
            },
            { 
                "channel:read:ads", 
                [
                    "channel.ad_break.begin"
                ] 
            },
            {
                "channel:manage:broadcast", 
                []
            },
            { 
                "channel:read:charity",
                [
                    "channel.charity_campaign.donate",
                    "channel.charity_campaign.start",
                    "channel.charity_campaign.progress",
                    "channel.charity_campaign.stop"
                ] 
            },
            {
                "channel:edit:commercial", 
                []
            },
            {
                "channel:read:editors", 
                []
            },
            {
                "channel:manage:extensions", 
                []
            },
            { 
                "channel:read:goals", 
                [
                    "channel.goal.begin",
                    "channel.goal.progress",
                    "channel.goal.end"
                ] 
            },
            { 
                "channel:read:guest_star", 
                [
                    "channel.guest_star_session.begin",
                    "channel.guest_star_session.end",
                    "channel.guest_star_guest.update",
                    "channel.guest_star_settings.update"
                ]
            },
            { 
                "channel:manage:guest_star", 
                [
                    "channel.guest_star_session.begin",
                    "channel.guest_star_session.end",
                    "channel.guest_star_guest.update",
                    "channel.guest_star_settings.update"
                ]
            },
            {
                "channel:read:hype_train", 
                [
                    "channel.hype_train.begin",
                    "channel.hype_train.progress",
                    "channel.hype_train.end"
                ]
            },
            {
                "channel:manage:moderators", 
                []
            },
            {
                "channel:read:polls", 
                [
                    "channel.poll.begin",
                    "channel.poll.progress",
                    "channel.poll.end"
                ]
            },
            {
                "channel:manage:polls", 
                [
                    "channel.poll.begin",
                    "channel.poll.progress",
                    "channel.poll.end"
                ]
            },
            {
                "channel:read:predictions", 
                [
                    "channel.prediction.begin",
                    "channel.prediction.progress",
                    "channel.prediction.lock",
                    "channel.prediction.end"
                ]
            },
            {
                "channel:manage:predictions", 
                [
                    "channel.prediction.begin",
                    "channel.prediction.progress",
                    "channel.prediction.lock",
                    "channel.prediction.end"
                ]
            },
            {
                "channel:manage:raids", 
                []
            },
            {
                "channel:read:redemptions", 
                [
                    "channel.channel_points_automatic_reward_redemption.add",
                    "channel.channel_points_custom_reward.add",
                    "channel.channel_points_custom_reward.update",
                    "channel.channel_points_custom_reward.remove",
                    "channel.channel_points_custom_reward_redemption.add",
                    "channel.channel_points_custom_reward_redemption.update"
                ]
            },
            {
                "channel:manage:redemptions",
                [
                    "channel.channel_points_automatic_reward_redemption.add",
                    "channel.channel_points_custom_reward.add",
                    "channel.channel_points_custom_reward.update",
                    "channel.channel_points_custom_reward.remove",
                    "channel.channel_points_custom_reward_redemption.add",
                    "channel.channel_points_custom_reward_redemption.update"
                ]
            },
            {
                "channel:manage:schedule", 
                []
            },
            {
                "channel:read:stream_key", 
                []
            },
            {
                "channel:read:subscriptions", 
                [
                    "channel.subscribe",
                    "channel.subscription.end",
                    "channel.subscription.gift",
                    "channel.subscription.message"
                ]
            },
            {
                "channel:manage:videos", 
                []
            },
            {
                "channel:read:vips", 
                [
                    "channel.vip.add",
                    "channel.vip.remove"
                ]
            },
            {
                "channel:manage:vips", 
                [
                    "channel.vip.add",
                    "channel.vip.remove"
                ]
            },
            {
                "channel:moderate", 
                [
                    "channel.ban",
                    "channel.unban"
                ]
            },
            {
                "clips:edit", 
                []
            },
            {
                "moderation:read", 
                [
                    "channel.moderator.add",
                    "channel.moderator.remove"
                ]
            },
            {
                "moderator:manage:announcements", 
                []
            },
            {
                "moderator:manage:automod", 
                [
                    "automod.message.hold",
                    "automod.message.update",
                    "automod.terms.update"
                ]
            },
            {
                "moderator:read:automod_settings", 
                [
                    "automod.settings.update"
                ]
            },
            {
                "moderator:manage:automod_settings", 
                []
            },
            {
                "moderator:read:banned_users", 
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:banned_users",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:blocked_terms",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:chat_messages",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:blocked_terms",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:chat_messages",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:chat_settings",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:chat_settings",
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:chatters", 
                []
            },
            {
                "moderator:read:followers", 
                [
                    "channel.follow"
                ]
            },
            {
                "moderator:read:guest_star", 
                [
                    "channel.guest_star_session.begin",
                    "channel.guest_star_session.end",
                    "channel.guest_star_guest.update",
                    "channel.guest_star_settings.update"
                ]
            },
            {
                "moderator:manage:guest_star", 
                [
                    "channel.guest_star_session.begin",
                    "channel.guest_star_session.end",
                    "channel.guest_star_guest.update",
                    "channel.guest_star_settings.update"
                ]
            },
            {
                "moderator:read:moderators", 
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:shield_mode", 
                [
                    "channel.shield_mode.begin",
                    "channel.shield_mode.end"
                ]
            },
            {
                "moderator:manage:shield_mode", 
                [
                    "channel.shield_mode.begin",
                    "channel.shield_mode.end"
                ]
            },
            {
                "moderator:read:shoutouts", 
                [
                    "channel.shoutout.create",
                    "channel.shoutout.receive"
                ]
            },
            {
                "moderator:manage:shoutouts", 
                [
                    "channel.shoutout.create",
                    "channel.shoutout.receive"
                ]
            },
            {
                "moderator:read:suspicious_users", 
                [
                    "channel.suspicious_user.message",
                    "channel.suspicious_user.update"
                ]
            },
            {
                "moderator:read:unban_requests", 
                [
                    "channel.unban_request.create",
                    "channel.unban_request.resolve",
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:unban_requests",
                [
                    "channel.unban_request.create",
                    "channel.unban_request.resolve",
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:vips", 
                [
                    "channel.moderate"
                ]
            },
            {
                "moderator:read:warnings", 
                [
                    "channel.warning.acknowledge",
                    "channel.warning.send",
                    "channel.moderate"
                ]
            },
            {
                "moderator:manage:warnings", 
                [
                    "channel.warning.acknowledge",
                    "channel.warning.send",
                    "channel.moderate"
                ]
            },
            {
                "user:bot", 
                [
                    "channel.chat.clear",
                    "channel.chat.clear_user_messages",
                    "channel.chat.message",
                    "channel.chat.message_delete",
                    "channel.chat.notification",
                    "channel.chat_settings.update",
                    "channel.chat.user_message_hold",
                    "channel.chat.user_message_update"
                ]
            },
            {
                "user:edit", 
                []
            },
            {
                "user:edit:broadcast", 
                []
            },
            {
                "user:read:blocked_users", 
                []
            },
            {
                "user:manage:blocked_users", 
                []
            },
            {
                "user:read:broadcast", 
                []
            },
            {
                "user:read:chat",
                [
                    "channel.chat.clear",
                    "channel.chat.clear_user_messages",
                    "channel.chat.message",
                    "channel.chat.message_delete",
                    "channel.chat.notification",
                    "channel.chat_settings.update",
                    "channel.chat.user_message_hold",
                    "channel.chat.user_message_update"
                ]
            },
            {
                "user:manage:chat_color", 
                []
            },
            {
                "user:read:email", 
                [
                    "user.update"
                ]
            },
            {
                "user:read:emotes", 
                []
            },
            {
                "user:read:follows", 
                []
            },
            {
                "user:read:moderated_channels", 
                []
            },
            {
                "user:read:subscriptions", 
                []
            },
            {
                "user:read:whispers", 
                [
                    "user.whisper.message"
                ]
            },
            {
                "user:manage:whispers", 
                [
                    "user.whisper.message"
                ]
            },
            {
                "user:write:chat", 
                []
            }
        };
        
        foreach (var scope in authScopeTypes)
        {
            var dbScope = dbScopes.FirstOrDefault(s => s.Name == scope.Key);
            if (dbScope is null)
                continue;

            foreach (var type in scope.Value)
            {
                var dbType = dbTypes.FirstOrDefault(s => s.Name == type);
                if (dbType is null)
                    continue;

                authScopeSubscriptionTypes.Add(new AuthorizationScopeSubscriptionType
                {
                    AuthorizationScopeId = dbScope.AuthorizationScopeId,
                    SubscriptionTypeId = dbType.SubscriptionTypeId
                });
            }
        }
        
        var dbRelationships = await neonContext.AuthorizationScopeSubscriptionType.ToListAsync(ct);
        if (dbRelationships.Count == 0)
        {
            //db has nothing, just add the range
            logger.LogDebug("No auth scope subscription types found in db, adding default types.");
            neonContext.AuthorizationScopeSubscriptionType.AddRange(authScopeSubscriptionTypes);
            await neonContext.SaveChangesAsync(ct);
            return;
        }
        
        //db has some data, check if we need to add any new relationships
        foreach (var relationship in authScopeSubscriptionTypes)
        {
            var dbRelationship = dbRelationships.FirstOrDefault(s => s.AuthorizationScopeId == relationship.AuthorizationScopeId && s.SubscriptionTypeId == relationship.SubscriptionTypeId);
            if (dbRelationship is null)
                neonContext.AuthorizationScopeSubscriptionType.Add(relationship);
        }
        
        //purge any types that are no longer in the list
        var removeList = dbRelationships.Where(s => authScopeSubscriptionTypes.All(t => t.AuthorizationScopeId != s.AuthorizationScopeId && t.SubscriptionTypeId != s.SubscriptionTypeId)).ToList();
        if (removeList.Count > 0)
        {
            neonContext.AuthorizationScopeSubscriptionType.RemoveRange(removeList);
            logger.LogDebug("Removed {count} auth scope subscription types from db.", removeList.Count);
        }
        
        if (neonContext.ChangeTracker.HasChanges())
            await neonContext.SaveChangesAsync(ct);
    }
}