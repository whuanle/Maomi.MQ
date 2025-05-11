# Maomi.MQ 　　　　　　　　　　　　　　　　　　　　[English](https://github.com/whuanle/Maomi.MQ/blob/main/README.md)

作者：痴者工良

文档地址：[https://mmq.whuanle.cn](https://mmq.whuanle.cn)

仓库地址：[https://github.com/whuanle/Maomi.MQ](https://github.com/whuanle/Maomi.MQ)

作者博客：

* [https://www.whuanle.cn](https://www.whuanle.cn)

* [https://www.cnblogs.com/whuanle](https://www.cnblogs.com/whuanle)



## 导读

Maomi.MQ 是一个消息通讯模型项目，目前只支持了 RabbitMQ。

Maomi.MQ.RabbitMQ 是一个用于专为 RabbitMQ 设计的发布者和消费者通讯模型，大大简化了发布和消息的代码，并提供一系列简便和实用的功能，开发者可以通过框架提供的消费模型实现高性能消费、事件编排，框架还支持发布者确认机制、自定义重试机制、补偿机制、死信队列、延迟队列、连接通道复用等一系列的便利功能。开发者可以把更多的精力放到业务逻辑中，通过 Maomi.MQ.RabbitMQ 框架简化跨进程消息通讯模式，使得跨进程消息传递更加简单和可靠。



此外，框架通过 runtime 内置的 api 支持了分布式可观测性，可以通过进一步使用 OpenTelemetry 等框架进一步收集可观测性信息，推送到基础设施平台中。



### 目录

* [快速入门](https://mmq.whuanle.cn/1.start.html)
* [发布事件](https://mmq.whuanle.cn/2.publisher.html)
* [消费者](https://mmq.whuanle.cn/2.0.consumer.html)
  - [消费者模式](https://mmq.whuanle.cn/2.1.consumer.html)
  - [事件总线模式](https://mmq.whuanle.cn/2.2.eventbus.html)
  - [自定义消费者和动态订阅](https://mmq.whuanle.cn/2.3.dynamic.md)
* [配置和调试](https://mmq.whuanle.cn/3.configuration.html)
* [Qos 并发和顺序](https://mmq.whuanle.cn/4.qos.html)
* [重试](https://mmq.whuanle.cn/5.retry.html)
* [死信队列](https://mmq.whuanle.cn/6.dead_queue.html)
* [可观测性](https://mmq.whuanle.cn/7.opentelemetry.html)
* [支持 MediatR、FastEndpoints](https://mmq.whuanle.cn/8.other_support.html) 