# 文档说明

作者：痴者工良

文档地址：[https://mmq.whuanle.cn](https://mmq.whuanle.cn)

仓库地址：[https://github.com/whuanle/Maomi.MQ](https://github.com/whuanle/Maomi.MQ)

作者博客：

* [https://www.whuanle.cn](https://www.whuanle.cn)

* [https://www.cnblogs.com/whuanle](https://www.cnblogs.com/whuanle)



## 导读

Maomi.MQ 是一个用于专为 RabbitMQ 设计的发布者和消费者通讯模型，大大简化了发布和消息的代码，并提供一系列简便和实用的功能，开发者可以通过框架提供的消费模型实现高性能消费、事件编排，框架还支持自定义重试机制、补偿机制、死信队列、延迟队列配置和处理，开发者可以把更多的精力放到业务逻辑中。

此外，框架通过 runtime 内置的 api 支持了分布式可观测性，可以通过进一步使用 OpenTelemetry 等框架进一步收集可观测性信息，推送到基础设施平台中。

> 后续将继续完善可观测性。



### 目录

* [快速入门](1.start.md) 

* [发布事件](2.publisher.md)
* [消费者](2.0.consumer.md)
  * [消费者模式](2.1.consumer.md)
  * [基于事件](2.2.eventbus.md)
  * [分组事件](2.3.event_group.md)
* [配置](3.configuration.md)
* [Qos 并发和顺序](4.qos.md)
* [重试](5.retry.md)
* [死信队列](6.dead_queue.md)
* [可观测性](7.opentelemtry.md)