using System;
using System.Collections.Generic;

namespace Messaging101
{
    public static class Program
    {
        public interface ICommand
        {
        }

        public class CheckItemOut : ICommand
        {
            public readonly uint Quantity;

            public CheckItemOut(uint quantity)
            {
                Quantity = quantity;
            }
        }

        public interface IHandle<in T> where T : ICommand
        {
            void Handle(T message);
        }

        public class LoggingHndler<T> : IHandle<T> where T : ICommand
        {
            private IHandle<T> next;

            public LoggingHndler(IHandle<T> next)
            {
                this.next = next;
            }

            public void Handle(T message)
            {
                Console.WriteLine($"About to do command: {message.GetType()}");
                next.Handle(message);
                Console.WriteLine($"Finished doing command: {message.GetType()}");
            }
        }

        public class CheckItemOUtHandler : IHandle<CheckItemOut>
        {
            private static List<IEvent> history = new List<IEvent>
            {
                new ItemCheckedIn(100),
                new ItemCheckedOut(50)
            };

            public void Handle(CheckItemOut message)
            {
                // Load object from history
                var obj = new InventoryBucket(history);

                // Call CheckItemOut
                obj.CheckItemOut(message.Quantity);

                var producedEvents = obj.GetProducedEvents();
                if (producedEvents.Count > 0)
                {
                    history.AddRange(producedEvents);

                    foreach (var e in producedEvents)
                    {
                        Console.WriteLine("Produced: " + e);
                    }
                }
            }

            public static void Main()
            {
                var checkItemOutPipeline =
                    new LoggingHndler<CheckItemOut>(
                        new CheckItemOUtHandler());


                checkItemOutPipeline.Handle(new CheckItemOut(10));

                var bucket = new InventoryBucket();
                bucket.CheckItemIn(100);
                bucket.CheckItemOut(10);
                bucket.CheckItemOut(10);
                bucket.CheckItemIn(100);

                var history = bucket.GetProducedEvents();
                //save in the database

                var item = new InventoryBucket(history);
                Console.WriteLine(item);
            }
        }

        public class InventoryBucket
        {
            private List<IEvent> producedEvents = new List<IEvent>();
            private uint quantityOnHand;

            public List<IEvent> GetProducedEvents()
            {
                var rtn = producedEvents;
                producedEvents = new List<IEvent>();
                return rtn;
            }

            public InventoryBucket(List<IEvent> history)
            {
                foreach (var @event in history)
                    Apply(@event);
            }

            public InventoryBucket()
            {
            }

            public override string ToString()
            {
                return $"Inventory Item: Count{quantityOnHand}";
            }

            private void RaiseEvent(IEvent @event)
            {
                producedEvents.Add(@event);
                Apply(@event);
            }

            private void Apply(IEvent @event)
            {
                if (@event is ItemCheckedIn itemCheckedIn)
                    quantityOnHand += itemCheckedIn.Quantity;
                else if (@event is ItemCheckedOut itemCheckedOut)
                    quantityOnHand -= itemCheckedOut.Quantity;
                else
                    throw new ArgumentException("Event type not matched.");
            }

            public void CheckItemIn(uint quantity)
            {
                if (quantity == 0)
                    throw new ArgumentException("quantity is 0");

                RaiseEvent(new ItemCheckedIn(quantity));
            }

            public void CheckItemOut(uint quantity)
            {
                if (quantity == 0)
                    throw new ArgumentException("quantity is 0");
                if (quantityOnHand < quantity)
                    throw new ArgumentException("The quantity on hand is less than the quantity requested.");

                RaiseEvent(new ItemCheckedOut(quantity));
            }
        }

        public interface IEvent
        {
        }

        public class ItemCheckedOut : IEvent
        {
            public readonly uint Quantity;

            public ItemCheckedOut(uint quantity)
            {
                Quantity = quantity;
            }
        }

        public class ItemCheckedIn : IEvent
        {
            public readonly uint Quantity;

            public ItemCheckedIn(uint quantity)
            {
                Quantity = quantity;
            }
        }

        public class InventoryBucketCreated : IEvent
        {
            public readonly string ItemName;
            public readonly uint OpeningCount;

            public InventoryBucketCreated(string itemName, uint openingCount)
            {
                ItemName = itemName;
                OpeningCount = openingCount;
            }
        }
    }
}