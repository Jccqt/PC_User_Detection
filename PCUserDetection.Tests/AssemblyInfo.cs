using Xunit;

// The settings file, the drop folder and the cooldown are all reached through
// static state, so two test classes running at once would be reading and
// writing each other's. Running them one at a time costs a fraction of a second
// on a suite this size and is the difference between a failure meaning
// something and a failure meaning two tests collided.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
