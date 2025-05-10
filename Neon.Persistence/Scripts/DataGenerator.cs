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
    }

    private async Task PreloadSubscriptionTypes(CancellationToken ct = default)
    {
        var subscriptionTypes = GetSubscriptionTypes();

        var dbTypes = await neonContext.SubscriptionType.ToListAsync(ct);

        if (dbTypes.Count == 0)
        {
            //db has nothing, just add the range
            logger.LogDebug("No subscription types found in db, adding default types.");
            await neonContext.SubscriptionType.AddRangeAsync(subscriptionTypes, ct);
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
}