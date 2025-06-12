Feature: ENSEK API Automation

  Background:
    Given the base API URL is configured
    When I fetch the list of available energy types
    Then I should store the energy response

    @FuelOrders
Scenario Outline: Place <fuel> order and verify message
  When I place an order for 1 unit of <fuel>
  And the order for <fuel> should return status code 200
  Then the purchase message should create an order
  And I fetch the order list
  And the placed order for <fuel> should appear in today's order list
  Examples:
  | fuel     |
  | gas      |   
  | oil      |
  | electric |

      
     @FuelOrders
Scenario: Count previous orders
    When I fetch the order list
    Then I should count orders placed before today

      @FuelOrders
Scenario: Invalid fuel type scenario
    When I query an order with invalid fuel type
    Then the response status code should be 405
    
  
 