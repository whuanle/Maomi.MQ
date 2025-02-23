

### 2.1.0 版本（规划中）

- [ ] 优化 RabbitMQ.Client.BasicProperties 内存占用。
- [ ] 增加分布式事务的支持模式。



### 2.0.1

- [x] 增加对 Protobuf 的支持
- [x] 增加多序列化器同时使用的支持
- [x] 修复部分单元测试错误



### 2.0.0

- [x] RabbitMQ.Client 升级到 7.0 正式版本

- [x] 删除 EventBusHostService

- [x] 删除 FirstHostService、WaitReadyHostService

- [x] DynamicConsumerTypeFilter 改名为 CustomConsumerTypeFilter

- [x] 动态消费者 DynamicConsumer 共用同一个 channel.

- [x] IJsonSerializer 改名为 IMessageSerializer

- [x] Eventbus 和 model comsuner 合二为一

- [x] `IConsumer.FallbackAsync()` 返回值从 bool 改成枚举

- [x] `IEventMiddleware.FallbackAsync()` 返回值改成枚举

- [x] 优化重构 Trace 和 Meter

- [x] 删除 IWaitReadyFactory

- [x] 删除 ConfirmPublisher，改成从创建时手动配置，[Issue #1682](https://github.com/rabbitmq/rabbitmq-dotnet-client/issues/1682) 、[RabbitMQ tutorial - Reliable Publishing with Publisher Confirms](https://www.rabbitmq.com/tutorials/tutorial-seven-dotnet)

- [x] 发布消息没有绑定队列时，触发 `BasicReturn` 事件，定义一个接口服务统一处理。

- [x] IConsumerOptions 增加交换器路由键。

- [x] 调整 ConsumerType、IConsumerOptions，将两者绑定在一起。

- [x] 创建交换器时，增加交换器处理接口，开发者可以设置定义模式，默认使用 fanout 绑定，可以设置使用其它模式绑定交换器。

- [x] 优化发布者，支持泛型发现，不需要指定交换器和路由。

- [x] 重新设计发布者、调整其它发布者接口类型

- [x] 添加专职发布者

- [x] 支持动态 EventBus，优化动态 `IConsumer<>`。

- [x] 发布消息以及接收消息时，通过 BasicProperties 、Header 优化信息传递，定制 BasicProperties 协议，以便后续编写 Go 版本。

- [x] 去掉 `EventBody<>`，将相关信息放到 header 中，以便更好地支持跨平台以及第三方序列化方式，添加 MessageHeader，将传递的消息协议都存储放在 Message 中。

- [x] 不在使用 Key 注入消费者服务和配置，改成

- [x] 支持直接使用函数式消费者，这个在动态消费者里面有部分代码了，可以结合一起使用。

- [x] 底层 TCP 连接错误或异常时，RabbitMQ Client 抛出错误，需要包装相关 API，自动重连、恢复。

  > [.NET/C# 客户端 API 指南 | RabbitMQ 消息队列](https://rabbitmq.cn/client-libraries/dotnet-api-guide#basic-return)
  
- [x] 找不到合适消费者 `IConsumer<TMessage>` 时，触发事件



### 1.3.0

* 去掉了连接池。
* 去掉了 IMessagePublisher 接口的 ConnectionPool 属性。
* 增加了动态消费者
* 增加动态订阅功能



### OpenTelemetry

使用时使用 ActivitySource、DiagnosticListener，然后使用 DiagnosticListener.Wirte 写入事件和参数

MaomiMQDiagnosticListener 监听事件，并处理，并且触发 MaomiMQInstrumentationEventSource 中的事件，记录信息。

> 将 DiagnosticSourceListener  合并到 MaomiMQDiagnosticListener 。



通过 MaomiMQInstrumentation 创建对 MaomiMQDiagnosticListener 的事件订阅
