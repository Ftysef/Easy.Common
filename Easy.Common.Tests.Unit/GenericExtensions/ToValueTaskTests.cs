﻿namespace Easy.Common.Tests.Unit.GenericExtensions
{
    using Easy.Common.Extensions;
    using NUnit.Framework;
    using Shouldly;

    [TestFixture]
    internal sealed class ToValueTaskTests
    {
        [Test]
        public void When_creating_a_task_from_reference_type()
        {
            const string SOURCE = "some-text";

            var task = SOURCE.ToValueTask();
            task.IsCompleted.ShouldBeTrue();
            task.IsCompletedSuccessfully.ShouldBeTrue();

            task.Result.ShouldBe(SOURCE);
        }

        [Test]
        public void When_creating_a_task_from_value_type()
        {
            const int SOURCE = 42;

            var task = SOURCE.ToValueTask();
            task.IsCompleted.ShouldBeTrue();
            task.IsCompletedSuccessfully.ShouldBeTrue();

            task.Result.ShouldBe(SOURCE);
        }

        [Test]
        public void When_creating_task_from_null_value()
        {
            const string SOURCE = null;

            var task = SOURCE.ToValueTask();
            task.IsCompleted.ShouldBeTrue();
            task.IsCompletedSuccessfully.ShouldBeTrue();

            task.Result.ShouldBe(SOURCE);
        }
    }
}